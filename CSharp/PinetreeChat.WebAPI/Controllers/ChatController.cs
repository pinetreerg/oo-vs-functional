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
        private ChatHubHelper _chatHub;

        public ChatController(IHubContext<ChatHub> chatHub, ChatService chatService)
        {
            _chatService = chatService;
            _chatHub = new ChatHubHelper(chatHub);
        }

        [HttpGet("list")]
        public IEnumerable<ChatDTO> List()
        {
            return _chatService.GetChats().Select(c => new ChatDTO(c)).ToList();
        }

        [HttpGet("{chatName}")]
        public IActionResult Get(string chatName)
        {
            var chat = _chatService.GetChat(chatName);
            if (chat == null)
            {
                return NotFound();
            }

            return Json(new ChatDTO(chat));
        }

        [HttpPost("create")]
        public IActionResult Create([FromBody]ChatDTO chatDto)
        {
            try
            {
                var chat = _chatService.CreateChat(chatDto.Name);
                _chatHub.ChatCreated(new ChatDTO(chat));
                return Ok();
            }
            catch (ChatExistsException ex)
            {
                Response.StatusCode = 409;
                return Json(ex.Message);
            }
            catch (ChatNameInvalidException ex)
            {
                Response.StatusCode = 400;
                return Json(ex.Message);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(ex.Message);
            }
        }

        [HttpPost("sendMessage")]
        public IActionResult SendMessage([FromBody]MessageDTO messageDto)
        {
            try
            {
                var message = _chatService.AddMessage(messageDto.ChatName, messageDto.Text, messageDto.From);
                _chatHub.MessageSent(new MessageDTO(messageDto.ChatName, message));
                return Ok();
            }
            catch (MessageInvalidException ex)
            {
                Response.StatusCode = 400;
                return Json(ex.Message);
            }
        }

        [HttpPost("leave")]
        public IActionResult LeaveChat([FromBody]LeaveDTO leaveDto)
        {
            _chatService.LeaveChat(leaveDto.ChatName, leaveDto.Participant);
            _chatHub.ChatLeft(leaveDto);

            return Ok();
        }

        private ChatService CreateChatService()
        {
            return new ChatService(new ChatRepository(), new UserRepository());
        }
    }
}
