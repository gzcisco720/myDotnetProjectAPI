using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using myDotnetApp.API.Data;
using myDotnetApp.API.Dtos;
using myDotnetApp.API.Helpers;
using myDotnetApp.API.Model;

namespace myDotnetApp.API.Controllers
{
    [Authorize]
    [ServiceFilter(typeof(LogUserActivity))]
    [Route("api/users/{userId}/[controller]")]
    public class MessagesController:Controller
    {
        private readonly IDatingRepository _repo;
        private readonly IMapper _map;

        public MessagesController(IDatingRepository repo, IMapper map)
        {
            _repo = repo;
            _map = map;
        }
        [HttpGet("{id}", Name = "GetMessage")]
        public async Task<IActionResult> GetMessage(int userId, int id)
        {
            if(userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
            {
                return Unauthorized();
            }
            var messageFromRepo = await _repo.GetMessage(id);
            if(messageFromRepo==null)
            {
                return NotFound();
            }
            return Ok(messageFromRepo);
        }
        [HttpGet("thread/{id}")]
        public async Task<IActionResult> GetMessageThread(int userId, int id)
        {
            if(userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
            {
                return Unauthorized();
            }
            var messageFromRepo = await _repo.GetMessageThread(userId,id);
            var messageThread = _map.Map<IEnumerable<MessageForReturnDto>>(messageFromRepo);
            return Ok(messageThread);
        }
        [HttpGet]
        public async Task<IActionResult> GetMessageForUser (int userId, MessageParams messageParams)
        {
            if(userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
            {
                return Unauthorized();
            }
            var messageFromRepo = await _repo.GetMessageForUser(messageParams);
            var messages = _map.Map<IEnumerable<MessageForReturnDto>>(messageFromRepo);
            Response.AddPagination(messageFromRepo.CurrentPage,messageFromRepo.PageSize,messageFromRepo.TotalCount,messageFromRepo.TotalPages);
            return Ok(messages);
        }
        [HttpPost]
        public async Task<IActionResult> CreateMessage(int userId, [FromBody] MessageForCreationDto messageForCreateionDto)
        {
            if(userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
            {
                return Unauthorized();
            }
            messageForCreateionDto.SenderId = userId;

            var recipient = await _repo.GetUser(messageForCreateionDto.RecipientId);
            var sender = await _repo.GetUser(messageForCreateionDto.SenderId);
            
            if(recipient == null)
            {
                return BadRequest("Could not find the user");
            }
            var message = _map.Map<Message>(messageForCreateionDto);
            _repo.Add(message);
            var messageToReturn = _map.Map<MessageForReturnDto>(message);
            if(await _repo.SaveAll())
            {
                return CreatedAtRoute("GetMessage", new { id = message.Id }, messageToReturn);
            }
            throw new Exception("Internal Server Error");
        }
        [HttpPost("{id}")]
        public async Task<IActionResult> DeleteMessage(int id, int userId)
        {
            if(userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
            {
                return Unauthorized();
            }
            var messageFromRepo = await _repo.GetMessage(id);
            if (messageFromRepo.SenderId == userId)
            {
                messageFromRepo.SenderDeleted = true;
            }
            if (messageFromRepo.RecipientId == userId)
            {
                messageFromRepo.RecipientDeleted = true;
            }
            if(messageFromRepo.SenderDeleted && messageFromRepo.RecipientDeleted)
            {
                _repo.Delete(messageFromRepo);
            }
            if(await _repo.SaveAll())
            {
                return NoContent();
            }
            throw new Exception("Internal Server Error");
        }
        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkMessageAsRead(int userId,int id)
        {
            if(userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
            {
                return Unauthorized();
            }
            var messageFromRepo = await _repo.GetMessage(id);
            if(messageFromRepo.RecipientId != userId)
            {
                return BadRequest("Error");
            }
            messageFromRepo.IsRead = true;
            messageFromRepo.DateRead = DateTime.Now;
            await _repo.SaveAll();
            return NoContent();
        }
    }
}