using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ErpNet.FP.PrintServer.Controllers
{
    /// <summary>
    /// Get a list of strings identified by ID
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class TodosController : ControllerBase
    {
        /// <summary>
        /// Request body for when a new request is to be formed
        /// </summary>
        public class NewTodo
        {
            /// <summary>
            /// Text of the TODO item
            /// </summary>
            [Required]
            public string Text;
        }

        /// <summary>
        /// TODO item
        /// </summary>
        public class Todo
        {
            /// <summary>
            /// Unique ID (Guid)
            /// </summary>
            [Required]
            public Guid Id;
            /// <summary>
            /// Text for the TODO item
            /// </summary>
            [Required]
            public string Text;
        }

        private static Dictionary<Guid, Todo> todos = new Dictionary<Guid, Todo>();

        /// <summary>
        /// Retrieves all todos
        /// </summary>
        /// <response code="200">Success</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<Todo>> GetAll()
        {
            return todos.Values;
        }

        /// <summary>
        /// Gets TODO by id
        /// </summary>
        /// <param name="id">Id of the TODO item</param>
        /// <response code="200">Success</response>
        /// <response code="400">Failed to convert <paramref name="id"/> to Guid</response>
        /// <response code="404">Failed to find TODO by ID</response>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<Todo> GetById(Guid id)
        {
            if (todos.TryGetValue(id, out var todo))
            {
                return todo;
            }
            else
            {
                return NotFound();
            }
        }

        /// <summary>
        /// Creates a new TODO
        /// </summary>
        /// <param name="value">Todo to create</param>
        /// <response code="200">Success</response>
        /// <response code="400">Failed to parse <paramref name="value"/> as TODO</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<Todo> Create([FromBody] NewTodo value)
        {
            var model = new Todo();
            model.Id = Guid.NewGuid();
            model.Text = value.Text;
            todos.Add(model.Id, model);
            return CreatedAtAction(nameof(GetById), new { id =model.Id } , value);
        }

        /// <summary>
        /// Update a TODO
        /// </summary>
        /// <param name="id">Id of the TODO to update</param>
        /// <param name="value">Todo to update</param>
        /// <response code="200">Success</response>
        /// <response code="400">Failed to parse <paramref name="value"/> as TODO or Todo.Id != <paramref name="id"/></response>
        /// /// <response code="404">Failed to find TODO by ID</response>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult Update(Guid id, [FromBody] Todo value)
        {
            if (id != value.Id) return BadRequest();
            if (todos.TryGetValue(id, out var existing))
            {
                existing.Text = value.Text;
                return Ok();
            }
            else
            {
                return NotFound();
            }
        }

        /// <summary>
        /// Delete a TODO by ID
        /// </summary>
        /// <param name="id">Id of the TODO to delete</param>
        /// <response code="200">Success</response>
        /// <response code="400">Failed to parse <paramref name="id"/> as Guid</response>
        /// <response code="404">Failed to find TODO by ID</response>
        // DELETE api/values/5
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult Delete(Guid id)
        {
            if (todos.Remove(id))
            {
                return Ok();
            }
            else
            {
                return NotFound();
            }
        }
    }
}
