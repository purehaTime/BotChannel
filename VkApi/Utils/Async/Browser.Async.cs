﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using JetBrains.Annotations;
using VkNet.Enums.Filters;
using VkNet.Enums.SafetyEnums;
using VkNet.Exception;
using VkNet.Model;

namespace VkNet.Utils
{
	/// <summary>
	/// Браузер
	/// </summary>
	public partial class Browser
	{
		/// <summary>
		/// Асинхронное получение json по url-адресу
		/// </summary>
		/// <param name="methodUrl"> Адрес получения json </param>
		/// <param name="parameters"> Параметры метода api </param>
		/// <returns> Строка в формате json </returns>
		public async Task<string> GetJsonAsync(string methodUrl, IEnumerable<KeyValuePair<string, string>> parameters)
		{
			return (await WebCall.PostCallAsync(url: methodUrl, parameters: parameters, webProxy: Proxy)).Response;
		}

		/// <summary>
		/// Асинхронная авторизация на сервере ВК
		/// </summary>
		/// <param name="authParams"> Параметры авторизации </param>
		/// <returns> Информация об авторизации приложения </returns>
		public async Task<VkAuthorization> AuthorizeAsync(IApiAuthParams authParams)
		{
			_logger?.Debug(message: "Шаг 1. Открытие диалога авторизации");
			var authorizeUrlResult = await OpenAuthDialogAsync(appId: authParams.ApplicationId, settings: authParams.Settings);

			if (IsAuthSuccessfull(webCallResult: authorizeUrlResult))
			{
				return await EndAuthorizeAsync(result: authorizeUrlResult, webProxy: Proxy);
			}

			_logger?.Debug(message: "Шаг 2. Заполнение формы логина");

			var loginFormPostResult = await FilledLoginFormAsync(email: authParams.Login
					, password: authParams.Password
					, captchaSid: authParams.CaptchaSid
					, captchaKey: authParams.CaptchaKey
					, authorizeUrlResult: authorizeUrlResult);

			if (IsAuthSuccessfull(webCallResult: loginFormPostResult))
			{
				return await EndAuthorizeAsync(result: loginFormPostResult, webProxy: Proxy);
			}

			if (HasNotTwoFactor(code: authParams.TwoFactorAuthorization, loginFormPostResult: loginFormPostResult))
			{
				return await EndAuthorizeAsync(result: loginFormPostResult, webProxy: Proxy);
			}

			_logger?.Debug(message: "Шаг 2.5.1. Заполнить код двухфакторной авторизации");

			var twoFactorFormResult =
					await FilledTwoFactorFormAsync(code: authParams.TwoFactorAuthorization, loginFormPostResult: loginFormPostResult);

			if (IsAuthSuccessfull(webCallResult: twoFactorFormResult))
			{
				return await EndAuthorizeAsync(result: twoFactorFormResult, webProxy: Proxy);
			}

			_logger?.Debug(message: "Шаг 2.5.2 Капча");
			var captchaForm = WebForm.From(result: twoFactorFormResult);

			var captcha = await WebCall.PostAsync(form: captchaForm, webProxy: Proxy);

			// todo: Нужно обработать капчу

			return await EndAuthorizeAsync(result: captcha, webProxy: Proxy);
		}

		/// <summary>
		/// Заполнить форму двухфакторной авторизации асинхронно
		/// </summary>
		/// <param name="code"> Функция возвращающая код двухфакторной авторизации </param>
		/// <param name="loginFormPostResult"> Ответ сервера vk </param>
		/// <returns> Ответ сервера vk </returns>
		private async Task<WebCallResult> FilledTwoFactorFormAsync(Func<string> code, WebCallResult loginFormPostResult)
		{
			var codeForm = WebForm.From(result: loginFormPostResult)
					.WithField(name: "code")
					.FilledWith(value: code.Invoke());

			return await WebCall.PostAsync(form: codeForm, webProxy: Proxy);
		}

		/// <summary>
		/// Заполнить форму логин и пароль асинхронно
		/// </summary>
		/// <param name="email"> Логин </param>
		/// <param name="password"> Пароль </param>
		/// <param name="captchaSid"> ИД капчи </param>
		/// <param name="captchaKey"> Значение капчи </param>
		/// <param name="authorizeUrlResult"> </param>
		/// <returns> </returns>
		private async Task<WebCallResult> FilledLoginFormAsync(string email
																, string password
																, long? captchaSid
																, string captchaKey
																, WebCallResult authorizeUrlResult)
		{
			var loginForm = WebForm.From(result: authorizeUrlResult)
					.WithField(name: "email")
					.FilledWith(value: email)
					.And()
					.WithField(name: "pass")
					.FilledWith(value: password);

			if (captchaSid.HasValue)
			{
				_logger?.Debug(message: "Шаг 2. Заполнение формы логина. Капча");

				loginForm.WithField(name: "captcha_sid")
						.FilledWith(value: captchaSid.Value.ToString())
						.WithField(name: "captcha_key")
						.FilledWith(value: captchaKey);
			}

			return await WebCall.PostAsync(form: loginForm, webProxy: Proxy);
		}

		/// <summary>
		/// Выполняет обход ошибки валидации асинхронно: https://vk.com/dev/need_validation
		/// </summary>
		/// <param name="validateUrl"> Адрес страницы валидации </param>
		/// <param name="phoneNumber">
		/// Номер телефона, который необходимо ввести на
		/// странице валидации
		/// </param>
		/// <returns> Информация об авторизации приложения. </returns>
		public async Task<VkAuthorization> ValidateAsync(string validateUrl, string phoneNumber)
		{
			if (string.IsNullOrWhiteSpace(value: validateUrl))
			{
				throw new ArgumentException(message: "Не задан адрес валидации!");
			}

			if (string.IsNullOrWhiteSpace(value: phoneNumber))
			{
				throw new ArgumentException(message: "Не задан номер телефона!");
			}

			var validateUrlResult = await WebCall.MakeCallAsync(url: validateUrl, webProxy: Proxy);

			var codeForm = WebForm.From(result: validateUrlResult)
					.WithField(name: "code")
					.FilledWith(value: phoneNumber.Substring(startIndex: 1, length: 8));

			var codeFormPostResult = await WebCall.PostAsync(form: codeForm, webProxy: Proxy);

			return await EndAuthorizeAsync(result: codeFormPostResult, webProxy: Proxy);
		}

		/// <summary>
		/// Закончить авторизацию асинхронно
		/// </summary>
		/// <param name="result"> Результат </param>
		/// <param name="webProxy"> Настройки прокси </param>
		/// <returns> </returns>
		/// <exception cref="CaptchaNeededException"> </exception>
		private async Task<VkAuthorization> EndAuthorizeAsync(WebCallResult result, IWebProxy webProxy = null)
		{
			if (IsAuthSuccessfull(webCallResult: result))
			{
				var auth = GetTokenUri(webCallResult: result);

				return VkAuthorization.From(uriFragment: auth.ToString());
			}

			if (HasСonfirmationRights(result: result))
			{
				_logger?.Debug(message: "Требуется подтверждение прав");
				var authorizationForm = WebForm.From(result: result);
				var authorizationFormPostResult = await WebCall.PostAsync(form: authorizationForm, webProxy: webProxy);

				if (!IsAuthSuccessfull(webCallResult: authorizationFormPostResult))
				{
					throw new VkApiException(message: "URI должен содержать токен!");
				}

				var tokenUri = GetTokenUri(webCallResult: authorizationFormPostResult);

				return VkAuthorization.From(uriFragment: tokenUri.ToString());
			}

			var captchaSid = HasCaptchaInput(result: result);

			if (!captchaSid.HasValue)
			{
				throw new VkApiException(message: "Непредвиденная ошибка авторизации. Обратитесь к разработчику.");
			}

			_logger?.Debug(message: "Требуется ввод капчи");

			throw new VkApiException(message: "Требуется ввод капчи");
		}

		/// <summary>
		/// Открытие окна авторизацииасинхронно
		/// </summary>
		/// <param name="appId"> id приложения </param>
		/// <param name="settings"> Настройки приложения </param>
		/// <returns> </returns>
		private async Task<WebCallResult> OpenAuthDialogAsync(ulong appId
															,  
															Settings settings)
		{
			var url = CreateAuthorizeUrlFor(appId: appId, settings: settings, display: Display.Page);

			return await WebCall.MakeCallAsync(url: url, webProxy: Proxy);
		}
	}
}