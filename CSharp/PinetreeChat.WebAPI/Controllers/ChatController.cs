﻿using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using PinetreeChat.Domain.Services;
using PinetreeChat.DataAccess.Repositories;
using System.Linq;
using PinetreeChat.WebAPI.DTOs;
using PinetreeChat.Domain.Services.Exceptions;
using Microsoft.AspNetCore.Http;
using System;
using PinetreeChat.WebAPI.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.SignalR;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace PinetreeChat.WebAPI.Controllers
{
    [Route("api/[controller]")]
    public class ChatController : Controller
    {
        private ChatService _chatService;
        private IHubContext<ChatHub> _chatHub;

        public ChatController(IHubContext<ChatHub> chatHub, ChatService chatService)
        {
            _chatService = chatService;
            _chatHub = chatHub;
        }

        [HttpGet("list")]
        public IEnumerable<ChatDTO> Get()
        {
            return _chatService.GetChats().Select(c => new ChatDTO(c)).ToList();
        }
        
        [HttpGet("{chatName}")]
        public IActionResult Get(string chatName)
        {
            var chat = _chatService.GetChat(chatName);
            if(chat == null)
            {
                return NotFound();
            }

            return Json(new ChatDTO(chat));
        }

        [HttpPost("create")]
        public IActionResult Post([FromBody]ChatDTO chatDto)
        {
            try
            {
                var chat = _chatService.CreateChat(chatDto.Name);
                _chatHub.Clients.All.InvokeAsync("ChatCreated", new ChatDTO(chat));
            }
            catch (ChatExistsException)
            {
                return new StatusCodeResult(StatusCodes.Status409Conflict);
            }
            catch (Exception)
            {
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            return Ok();
        }

        [HttpPost("sendMessage")]
        public IActionResult SendMessage([FromBody]MessageDTO messageDto)
        {
            var message = _chatService.AddMessage(messageDto.ChatName, messageDto.Text, messageDto.From);
            _chatHub.Clients.All.InvokeAsync("MessageSent", new MessageDTO(messageDto.ChatName, message));

            return Ok();
        }


        [HttpPost("leave")]
        public IActionResult LeaveChat([FromBody]LeaveDTO leaveDto)
        {
            _chatService.LeaveChat(leaveDto.ChatName, leaveDto.Participant);
            _chatHub.Clients.All.InvokeAsync("ChatLeft", leaveDto);

            return Ok();
        }

        private ChatService CreateChatService()
        {
            return new ChatService(new ChatRepository(), new UserRepository());
        }
    }
}
