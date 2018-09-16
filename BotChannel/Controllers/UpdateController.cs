﻿
using System.Threading.Tasks;
using BotChannel.Services;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;

namespace BotChannel.Controllers
{
	[Route("api/[controller]")]
	public class UpdateController : Controller
	{
		private readonly IUpdateService _updateService;

		public UpdateController(IUpdateService updateService)
		{
			_updateService = updateService;
		}

		// POST api/update
		[HttpPost]
		public async Task<IActionResult> Post([FromBody]Update update)
		{
			await _updateService.EchoAsync(update);
			return Ok();
		}
	}
}
