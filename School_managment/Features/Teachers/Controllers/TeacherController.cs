using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_managment.Features.Teachers.DTOs;
using School_managment.Features.Teachers.Models;
using School_managment.Features.Teachers.Orchestrators;
using School_managment.Features.Teachers.Queries;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace School_managment.Features.Teachers.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TeacherController : ControllerBase
    {
        private readonly TeacherOrchestrator _orchestrator;

        public TeacherController(TeacherOrchestrator orchestrator)
        {
            _orchestrator = orchestrator;
        }

        [HttpGet]
        public async Task<ActionResult<List<TeacherDto>>> GetAll()
        {
            var teachers = await _orchestrator.GetAllTeachersAsync();
            return Ok(teachers);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TeacherDto>> GetById(int id)
        {
            var teacher = await _orchestrator.GetTeacherByIdAsync(id);
            if (teacher == null)
                return NotFound();

            return Ok(teacher);
        }

        [HttpPost]
        [HttpPost]
        public async Task<ActionResult<TeacherDto>> Add([FromBody] AddTeacherDto dto)
        {
            var createdTeacherResponse = await _orchestrator.AddTeacherAsync(dto);

            if (!createdTeacherResponse.IsSuccess || createdTeacherResponse.Data == null)
                return BadRequest(createdTeacherResponse);

            return CreatedAtAction(
                nameof(GetById),
                new { id = createdTeacherResponse.Data.Id },
                createdTeacherResponse.Data
            );
        }


        [HttpPut("{id}")]
        public async Task<ActionResult<TeacherDto>> Update(int id, [FromBody] UpdateTeacherDto dto)
        {
            if (id != dto.Id)
                return BadRequest("Id mismatch");

            var updatedTeacher = await _orchestrator.UpdateTeacherAsync(dto);
            if (updatedTeacher == null)
                return NotFound();

            return Ok(updatedTeacher);
        }

        [HttpGet("paged")]
        public async Task<IActionResult> GetTeachers(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null)
        {
            var result = await _orchestrator.GetTeachersAsync(pageNumber, pageSize, searchTerm);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var response = await _orchestrator.DeleteTeacherAsync(id);

            if (!response.IsSuccess || !response.Data)
                return NotFound(response);

            return NoContent();
        }

    }

}

