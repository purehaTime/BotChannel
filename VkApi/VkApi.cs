// ReSharper disable once RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using NLog;
using VkNet.Abstractions;
using VkNet.Abstractions.Utils;
using VkNet.Categories;
using VkNet.Enums;
using VkNet.Exception;
using VkNet.Model;
using VkNet.Utils;
using VkNet.Utils.AntiCaptcha;
using VkNet.Utils.JsonConverter;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable EventNeverSubscribedTo.Global

namespace VkNet
{
	/// <summary>
	/// Служит для оповещения об истечении токена
	/// </summary>
	/// <param name="sender">
	/// Экземпляр API у которого истекло время токена
	/// </param>
	public delegate void VkApiDelegate(VkApi sender);

	/// <inheritdoc />
	/// <summary>
	/// API для работы с ВКонтакте.
	/// Выступает в качестве фабрики для различных категорий API (например, для работы
	/// с пользователями, группами и т.п.)
	/// </summary>
	public class VkApi : IVkApi
	{
		/// <summary>
		/// Версия API vk.com.
		/// </summary>
		public const string VkApiVersion = "5.78";

		/// <summary>
		/// The expire timer lock
		/// </summary>
		private readonly object _expireTimerLock = new object();

		/// <summary>
		/// Параметры авторизации.
		/// </summary>
		private IApiAuthParams _ap;

		/// <summary>
		/// Таймер.
		/// </summary>
		private Timer _expireTimer;

		/// <summary>
		/// Логгер
		/// </summary>
		private ILogger _logger;

	#pragma warning disable S1104 // Fields should not have public accessibility
		/// <summary>
		/// Rest Client
		/// </summary>
		public IRestClient RestClient;
	#pragma warning restore S1104 // Fields should not have public accessibility

		/// <inheritdoc />
		public VkApi(ILogger logger, ICaptchaSolver captchaSolver = null, IBrowser browser = null)
		{
			var container = new ServiceCollection();

			if (logger != null)
			{
				container.TryAddSingleton(instance: logger);
			}

			if (captchaSolver != null)
			{
				container.TryAddSingleton(instance: captchaSolver);
			}

			if (browser != null)
			{
				container.TryAddSingleton(instance: browser);
			}

			container.RegisterDefaultDependencies();

			IServiceProvider serviceProvider = container.BuildServiceProvider();

			Initialization(serviceProvider: serviceProvider);
		}

		/// <inheritdoc />
		public VkApi(IServiceCollection serviceCollection = null)
		{
			var container = serviceCollection ?? new ServiceCollection();

			container.RegisterDefaultDependencies();

			IServiceProvider serviceProvider = container.BuildServiceProvider();

			Initialization(serviceProvider: serviceProvider);
		}

		/// <summary>
		/// Токен для доступа к методам API
		/// </summary>
		private string AccessToken { get; set; }

		/// <summary>
		/// Язык получаемых данных
		/// </summary>
		private Language? Language { get; set; }

		/// <inheritdoc />
		public event VkApiDelegate OnTokenExpires;

		/// <inheritdoc />
		public IBrowser Browser { get; set; }

		/// <inheritdoc />
		public bool IsAuthorized => !string.IsNullOrWhiteSpace(value: AccessToken);

		/// <inheritdoc />
		public string Token => AccessToken;

		/// <inheritdoc />
		public long? UserId { get; set; }

		/// <inheritdoc />
		public int MaxCaptchaRecognitionCount { get; set; }

		/// <inheritdoc />
		public ICaptchaSolver CaptchaSolver { get; set; }

		/// <inheritdoc />
		public void SetLanguage(Language language)
		{
			Language = language;
		}

		/// <inheritdoc />
		public Language? GetLanguage()
		{
			return Language;
		}

		/// <inheritdoc />
		public void Authorize(IApiAuthParams @params)
		{
			// подключение браузера через прокси
			if (@params.Host != null)
			{
				_logger?.Debug(message: "Настройка прокси");

				Browser.Proxy = WebProxy.GetProxy(host: @params.Host
						, port: @params.Port
						, proxyLogin: @params.ProxyLogin
						, proxyPassword: @params.ProxyPassword);

				RestClient.Proxy = Browser.Proxy;
			}

			// если токен не задан - обычная авторизация
			if (@params.AccessToken == null)
			{
				AuthorizeWithAntiCaptcha(authParams: @params);

				// Сбросить после использования
				@params.CaptchaSid = null;
				@params.CaptchaKey = "";
			}

			// если токен задан - авторизация с помощью токена полученного извне
			else
			{
				TokenAuth(accessToken: @params.AccessToken, userId: @params.UserId, expireTime: @params.TokenExpireTime);
			}

			_ap = @params;
			_logger?.Debug(message: "Авторизация прошла успешно");
		}

		/// <inheritdoc />
		public async Task AuthorizeAsync(IApiAuthParams @params)
		{
			await TypeHelper.TryInvokeMethodAsync(func: () => Authorize(@params: @params));
		}

		/// <inheritdoc />
		public void RefreshToken(Func<string> code = null)
		{
			if (!string.IsNullOrWhiteSpace(value: _ap.Login) && !string.IsNullOrWhiteSpace(value: _ap.Password))
			{
				_ap.TwoFactorAuthorization = _ap.TwoFactorAuthorization ?? code;
				AuthorizeWithAntiCaptcha(authParams: _ap);
			} else
			{
				const string message =
						"Невозможно обновить токен доступа т.к. последняя авторизация происходила не при помощи логина и пароля";

				_logger?.Error(message: message);

				throw new AggregateException(message: message);
			}
		}

		/// <inheritdoc />
		public async Task RefreshTokenAsync(Func<string> code = null)
		{
			await TypeHelper.TryInvokeMethodAsync(func: () => RefreshToken(code: code));
		}

		/// <inheritdoc />
		[MethodImpl(methodImplOptions: MethodImplOptions.NoInlining)]
		public VkResponse Call(string methodName, VkParameters parameters, bool skipAuthorization = false)
		{
			var answer = CallBase(methodName: methodName, parameters: parameters, skipAuthorization: skipAuthorization);

			var json = JObject.Parse(json: answer);

			var rawResponse = json[propertyName: "response"];

			return new VkResponse(token: rawResponse) { RawJson = answer };
		}

		/// <inheritdoc />
		public async Task<VkResponse> CallAsync(string methodName, VkParameters parameters, bool skipAuthorization = false)
		{
			return await TypeHelper.TryInvokeMethodAsync(func: () =>
					Call(methodName: methodName, parameters: parameters, skipAuthorization: skipAuthorization));
		}

		/// <inheritdoc />
		public async Task<T> CallAsync<T>(string methodName, VkParameters parameters, bool skipAuthorization = false)
		{
			return await TypeHelper.TryInvokeMethodAsync(func: () =>
					Call<T>(methodName: methodName, parameters: parameters, skipAuthorization: skipAuthorization));
		}

		/// <inheritdoc />
		[MethodImpl(methodImplOptions: MethodImplOptions.NoInlining)]
		public T Call<T>(string methodName, VkParameters parameters, bool skipAuthorization = false, params JsonConverter[] jsonConverters)
		{
			var answer = CallBase(methodName: methodName, parameters: parameters, skipAuthorization: skipAuthorization);

			if (!jsonConverters.Any())
			{
				return JsonConvert.DeserializeObject<T>(answer
						, new VkCollectionJsonConverter()
						, new VkDefaultJsonConverter()
						, new UnixDateTimeConverter()
						, new AttachmentJsonConverter()
						, new StringEnumConverter());
			}

			return JsonConvert.DeserializeObject<T>(value: answer, converters: jsonConverters);
		}

		/// <inheritdoc />
		public string Invoke(string methodName, IDictionary<string, string> parameters, bool skipAuthorization = false)
		{
			if (!skipAuthorization && !IsAuthorized)
			{
				var message = $"Метод '{methodName}' нельзя вызывать без авторизации";
				_logger?.Error(message: message);

				throw new AccessTokenInvalidException(message: message);
			}

			var url = $"https://api.vk.com/method/{methodName}";
			var answer = "";

			void SendRequest(string method, IDictionary<string, string> @params)
			{
				LastInvokeTime = DateTimeOffset.Now;

				var response = RestClient.PostAsync(uri: new Uri(uriString: $"https://api.vk.com/method/{method}"), parameters: @params)
						.Result;

				answer = response.Value ?? response.Message;
			}

			// Защита от превышения количества запросов в секунду
			if (RequestsPerSecond > 0 && LastInvokeTime.HasValue)
			{
				if (_expireTimer == null)
				{
					SetTimer(expireTime: 0);
				}

				lock (_expireTimerLock)
				{
					var span = LastInvokeTimeSpan?.TotalMilliseconds;

					if (span < _minInterval)
					{
						var timeout = (int) _minInterval - (int) span;
					#if NET40
						Thread.Sleep(millisecondsTimeout: timeout);
					#else
						Task.Delay(millisecondsDelay: timeout).Wait();
					#endif
					}

					SendRequest(method: methodName, @params: parameters);
				}
			} else if (skipAuthorization)
			{
				SendRequest(method: methodName, @params: parameters);
			}

			_logger?.Trace(message: $"Uri = \"{url}\"");
			_logger?.Trace(message: $"Json ={Environment.NewLine}{Utilities.PreetyPrintJson(json: answer)}");

			VkErrors.IfErrorThrowException(json: answer);

			return answer;
		}

		/// <inheritdoc />
		 
		public async Task<string> InvokeAsync(string methodName, IDictionary<string, string> parameters, bool skipAuthorization = false)
		{
			return await TypeHelper.TryInvokeMethodAsync(func: () =>
					Invoke(methodName: methodName, parameters: parameters, skipAuthorization: skipAuthorization));
		}

		/// <inheritdoc cref="IDisposable" />
		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(obj: this);
		}

		/// <inheritdoc />
		public void Validate(string validateUrl, string phoneNumber)
		{
			StopTimer();

			LastInvokeTime = DateTimeOffset.Now;
			var authorization = Browser.Validate(validateUrl: validateUrl, phoneNumber: phoneNumber);

			if (!authorization.IsAuthorized)
			{
				const string message = "Не удалось автоматически пройти валидацию!";
				_logger?.Error(message: message);

				throw new NeedValidationException(message: message, strRedirectUri: validateUrl);
			}

			AccessToken = authorization.AccessToken;
			UserId = authorization.UserId;
		}

		/// <summary>
		/// Releases unmanaged and - optionally - managed resources.
		/// </summary>
		/// <param name="disposing">
		/// <c> true </c> to release both managed and unmanaged resources; <c> false </c>
		/// to release only
		/// unmanaged resources.
		/// </param>
		protected virtual void Dispose(bool disposing)
		{
			_expireTimer?.Dispose();
		}

	#region Requests limit stuff

		/// <summary>
		/// Запросов в секунду.
		/// </summary>
		private float _requestsPerSecond;

		/// <summary>
		/// Минимальное время, которое должно пройти между запросами чтобы не превысить
		/// кол-во запросов в секунду.
		/// </summary>
		private float _minInterval;

		/// <inheritdoc />
		public DateTimeOffset? LastInvokeTime { get; private set; }

		/// <inheritdoc />
		public TimeSpan? LastInvokeTimeSpan
		{
			get
			{
				if (LastInvokeTime.HasValue)
				{
					return DateTimeOffset.Now - LastInvokeTime.Value;
				}

				return null;
			}
		}

		private readonly object _minIntervalLock = new object();

		/// <inheritdoc />
		public float RequestsPerSecond
		{
			get => _requestsPerSecond;
			set
			{
				if (value < 0)
				{
					throw new ArgumentException(message: @"Value must be positive", paramName: nameof(value));
				}

				_requestsPerSecond = value;

				if (_requestsPerSecond <= 0)
				{
					return;
				}

				lock (_minIntervalLock)
				{
					_minInterval = (int) (1000 / _requestsPerSecond) + 1;
				}
			}
		}

	#endregion

	#region Categories Definition

		/// <inheritdoc />
		public IUsersCategory Users { get; private set; }

		/// <inheritdoc />
		public IFriendsCategory Friends { get; private set; }

		/// <inheritdoc />
		public IStatusCategory Status { get; private set; }

		/// <inheritdoc />
		public IMessagesCategory Messages { get; private set; }

		/// <inheritdoc />
		public IGroupsCategory Groups { get; private set; }

		/// <inheritdoc />
		public AudioCategory Audio { get; private set; }

		/// <inheritdoc />
		public IDatabaseCategory Database { get; private set; }

		/// <inheritdoc />
		public IUtilsCategory Utils { get; private set; }

		/// <inheritdoc />
		public IWallCategory Wall { get; private set; }

		/// <inheritdoc />
		public IBoardCategory Board { get; private set; }

		/// <inheritdoc />
		public IFaveCategory Fave { get; private set; }

		/// <inheritdoc />
		public IVideoCategory Video { get; private set; }

		/// <inheritdoc />
		public IAccountCategory Account { get; private set; }

		/// <inheritdoc />
		public IPhotoCategory Photo { get; private set; }

		/// <inheritdoc />
		public IDocsCategory Docs { get; private set; }

		/// <inheritdoc />
		public ILikesCategory Likes { get; private set; }

		/// <inheritdoc />
		public IPagesCategory Pages { get; private set; }

		/// <inheritdoc />
		public IAppsCategory Apps { get; private set; }

		/// <inheritdoc />
		public INewsFeedCategory NewsFeed { get; private set; }

		/// <inheritdoc />
		public IStatsCategory Stats { get; private set; }

		/// <inheritdoc />
		public IGiftsCategory Gifts { get; private set; }

		/// <inheritdoc />
		public IMarketsCategory Markets { get; private set; }

		/// <inheritdoc />
		public IAuthCategory Auth { get; private set; }

		/// <inheritdoc />
		public IExecuteCategory Execute { get; private set; }

		/// <inheritdoc />
		public IPollsCategory PollsCategory { get; private set; }

		/// <inheritdoc />
		public ISearchCategory Search { get; private set; }

		/// <inheritdoc />
		public IStorageCategory Storage { get; set; }

		/// <inheritdoc />
		public IAdsCategory Ads { get; private set; }

		/// <inheritdoc />
		public INotificationsCategory Notifications { get; set; }

		/// <inheritdoc />
		public IWidgetsCategory Widgets { get; set; }

		/// <inheritdoc />
		public ILeadsCategory Leads { get; set; }

		/// <inheritdoc />
		public IStreamingCategory Streaming { get; set; }

		/// <inheritdoc />
		public IPlacesCategory Places { get; set; }

	#endregion

	#region private

		/// <summary>
		/// Базовое обращение к vk.com
		/// </summary>
		/// <param name="methodName"> Наименование метода </param>
		/// <param name="parameters"> Параметры запроса </param>
		/// <param name="skipAuthorization"> Пропустить авторизацию </param>
		/// <returns> Ответ от vk.com в формате json </returns>
		/// <exception cref="CaptchaNeededException"> Требуется ввести капчу </exception>
		private string CallBase(string methodName, VkParameters parameters, bool skipAuthorization)
		{
			if (!parameters.ContainsKey(key: "v"))
			{
				parameters.Add(name: "v", value: VkApiVersion);
			}

			if (!parameters.ContainsKey(key: "access_token"))
			{
				parameters.Add(name: "access_token", value: AccessToken);
			}

			if (!parameters.ContainsKey(key: "lang") && Language.HasValue)
			{
				parameters.Add(name: "lang", nullableValue: Language);
			}

			_logger?.Debug(message:
					$"Вызов метода {methodName}, с параметрами {string.Join(separator: ",", values: parameters.Select(selector: x => $"{x.Key}={x.Value}"))}");

			string answer;

			if (CaptchaSolver == null)
			{
				answer = Invoke(methodName: methodName, parameters: parameters, skipAuthorization: skipAuthorization);
			} else
			{
				answer = CaptchaHandler(action: (sid, key) =>
				{
					parameters.Add(name: "captcha_sid", nullableValue: sid);
					parameters.Add(name: "captcha_key", value: key);

					return Invoke(methodName: methodName, parameters: parameters, skipAuthorization: skipAuthorization);
				});
			}

			return answer;
		}

		/// <summary>
		/// Авторизация и получение токена
		/// </summary>
		/// <param name="authParams"> Параметры авторизации </param>
		/// <exception cref="VkApiAuthorizationException"> </exception>
		private void AuthorizeWithAntiCaptcha(IApiAuthParams authParams)
		{
			_logger?.Debug(message: "Старт авторизации");

			if (CaptchaSolver == null)
			{
				BaseAuthorize(authParams: authParams);
			} else
			{
				CaptchaHandler(action: (sid, key) =>
				{
					_logger?.Debug(message: "Авторизация с использование капчи.");
					authParams.CaptchaSid = sid;
					authParams.CaptchaKey = key;
					BaseAuthorize(authParams: authParams);

					return true;
				});
			}
		}

		/// <summary>
		/// Обработка капчи
		/// </summary>
		/// <param name="action"> Действие </param>
		/// <typeparam name="T"> Тип результата </typeparam>
		/// <returns> Результат действия </returns>
		/// <exception cref="CaptchaNeededException"> Требуется обработка капчи. </exception>
		private T CaptchaHandler<T>(Func<long?, string, T> action)
		{
			var numberOfRemainingAttemptsToSolveCaptcha = MaxCaptchaRecognitionCount;
			var numberOfRemainingAttemptsToAuthorize = MaxCaptchaRecognitionCount + 1;
			long? captchaSidTemp = null;
			string captchaKeyTemp = null;
			var callCompleted = false;
			var result = default(T);

			do
			{
				try
				{
					result = action.Invoke(arg1: captchaSidTemp, arg2: captchaKeyTemp);
					numberOfRemainingAttemptsToAuthorize--;
					callCompleted = true;
				}
				catch (CaptchaNeededException captchaNeededException)
				{
					RepeatSolveCaptcha(numberOfRemainingAttemptsToSolveCaptcha: ref numberOfRemainingAttemptsToSolveCaptcha
							, captchaNeededException: captchaNeededException
							, captchaSidTemp: ref captchaSidTemp
							, captchaKeyTemp: ref captchaKeyTemp);
				}
			} while (numberOfRemainingAttemptsToAuthorize > 0 && !callCompleted);

			// Повторно выбрасываем исключение, если капча ни разу не была распознана верно
			if (callCompleted || !captchaSidTemp.HasValue)
			{
				return result;
			}

			_logger?.Error(message: "Капча ни разу не была распознана верно");

			throw new CaptchaNeededException(sid: captchaSidTemp.Value, img: captchaKeyTemp);
		}

		/// <summary>
		/// Авторизация через установку токена
		/// </summary>
		/// <param name="accessToken"> Токен </param>
		/// <param name="userId"> Идентификатор пользователя </param>
		/// <param name="expireTime"> Время истечения токена </param>
		/// <exception cref="ArgumentNullException"> </exception>
		private void TokenAuth(string accessToken, long? userId, int expireTime)
		{
			if (string.IsNullOrWhiteSpace(value: accessToken))
			{
				_logger?.Error(message: "Авторизация через токен. Токен не задан.");

				throw new ArgumentNullException(paramName: accessToken);
			}

			_logger?.Debug(message: "Авторизация через токен");
			StopTimer();

			LastInvokeTime = DateTimeOffset.Now;
			SetApiPropertiesAfterAuth(expireTime: expireTime, accessToken: accessToken, userId: userId);
			_ap = new ApiAuthParams();
		}

		/// <summary>
		/// Sets the token properties.
		/// </summary>
		/// <param name="authorization"> The authorization. </param>
		private void SetTokenProperties(VkAuthorization authorization)
		{
			_logger?.Debug(message: "Установка свойств токена");
			var expireTime = (Convert.ToInt32(value: authorization.ExpiresIn) - 10) * 1000;
			SetApiPropertiesAfterAuth(expireTime: expireTime, accessToken: authorization.AccessToken, userId: authorization.UserId);
		}

		/// <summary>
		/// Установить свойства api после авторизации
		/// </summary>
		/// <param name="expireTime"> </param>
		/// <param name="accessToken"> </param>
		/// <param name="userId"> </param>
		private void SetApiPropertiesAfterAuth(int expireTime, string accessToken, long? userId)
		{
			SetTimer(expireTime: expireTime);
			AccessToken = accessToken;
			UserId = userId;
		}

		/// <summary>
		/// Повторная обработка капчи
		/// </summary>
		/// <param name="numberOfRemainingAttemptsToSolveCaptcha"> </param>
		/// <param name="captchaNeededException"> </param>
		/// <param name="captchaSidTemp"> </param>
		/// <param name="captchaKeyTemp"> </param>
		private void RepeatSolveCaptcha(ref int numberOfRemainingAttemptsToSolveCaptcha
										, CaptchaNeededException captchaNeededException
										, ref long? captchaSidTemp
										, ref string captchaKeyTemp)
		{
			_logger?.Warn(message: "Повторная обработка капчи");

			if (numberOfRemainingAttemptsToSolveCaptcha < MaxCaptchaRecognitionCount)
			{
				CaptchaSolver?.CaptchaIsFalse();
			}

			if (numberOfRemainingAttemptsToSolveCaptcha <= 0)
			{
				return;
			}

			captchaSidTemp = captchaNeededException.Sid;
			captchaKeyTemp = CaptchaSolver?.Solve(url: captchaNeededException.Img?.AbsoluteUri);
			numberOfRemainingAttemptsToSolveCaptcha--;
		}

		/// <summary>
		/// Установить значение таймера
		/// </summary>
		/// <param name="expireTime"> Значение таймера </param>
		private void SetTimer(int expireTime)
		{
			_expireTimer = new Timer(callback: AlertExpires
					, state: null
					, dueTime: expireTime > 0 ? expireTime : Timeout.Infinite
					, period: Timeout.Infinite);
		}

		/// <summary>
		/// Прекращает работу таймера оповещения
		/// </summary>
		private void StopTimer()
		{
			_expireTimer?.Dispose();
		}

		/// <summary>
		/// Создает событие оповещения об окончании времени токена
		/// </summary>
		/// <param name="state"> </param>
		private void AlertExpires(object state)
		{
			OnTokenExpires?.Invoke(sender: this);
		}

		/// <summary>
		/// Авторизация и получение токена
		/// </summary>
		/// <param name="authParams"> Параметры авторизации </param>
		/// <exception cref="VkApiAuthorizationException"> </exception>
		private void BaseAuthorize(IApiAuthParams authParams)
		{
			StopTimer();

			LastInvokeTime = DateTimeOffset.Now;
			var authorization = Browser.Authorize(authParams: authParams);

			if (!authorization.IsAuthorized)
			{
				var message = $"Invalid authorization with {authParams.Login} - {authParams.Password}";
				_logger?.Error(message: message);

				throw new VkApiAuthorizationException(message: message, email: authParams.Login, password: authParams.Password);
			}

			SetTokenProperties(authorization: authorization);
		}

		private void Initialization(IServiceProvider serviceProvider)
		{
			Browser = serviceProvider.GetRequiredService<IBrowser>();
			CaptchaSolver = serviceProvider.GetService<ICaptchaSolver>();
			_logger = serviceProvider.GetService<ILogger>();
			RestClient = serviceProvider.GetRequiredService<IRestClient>();
			Users = new UsersCategory(vk: this);
			Friends = new FriendsCategory(vk: this);
			Status = new StatusCategory(vk: this);
			Messages = new MessagesCategory(vk: this);
			Groups = new GroupsCategory(vk: this);
			Audio = new AudioCategory(vk: this);
			Wall = new WallCategory(vk: this);
			Board = new BoardCategory(vk: this);
			Database = new DatabaseCategory(vk: this);
			Utils = new UtilsCategory(vk: this);
			Fave = new FaveCategory(vk: this);
			Video = new VideoCategory(vk: this);
			Account = new AccountCategory(vk: this);
			Photo = new PhotoCategory(vk: this);
			Docs = new DocsCategory(vk: this);
			Likes = new LikesCategory(vk: this);
			Pages = new PagesCategory(vk: this);
			Gifts = new GiftsCategory(vk: this);
			Apps = new AppsCategory(vk: this);
			NewsFeed = new NewsFeedCategory(vk: this);
			Stats = new StatsCategory(vk: this);
			Auth = new AuthCategory(vk: this);
			Markets = new MarketsCategory(vk: this);
			Execute = new ExecuteCategory(vk: this);
			PollsCategory = new PollsCategory(vk: this);
			Search = new SearchCategory(vk: this);
			Ads = new AdsCategory(vk: this);
			Storage = new StorageCategory(api: this);
			Notifications = new NotificationsCategory(api: this);
			Widgets = new WidgetsCategory(api: this);
			Leads = new LeadsCategory(api: this);
			Streaming = new StreamingCategory(api: this);
			Places = new PlacesCategory(api: this);

			RequestsPerSecond = 3;

			MaxCaptchaRecognitionCount = 5;
			_logger?.Debug(message: "VkApi Initialization successfully");
		}

	#endregion
	}
}