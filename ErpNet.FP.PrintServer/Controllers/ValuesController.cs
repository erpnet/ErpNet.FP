using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace ErpNet.FP.PrintServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        public class Todo
        {
            public Guid? Id;
            public string Value;
        }

        private static Dictionary<Guid, Todo> todos = new Dictionary<Guid, Todo>();

        // GET api/values
        [HttpGet]
        public ActionResult<IEnumerable<Todo>> Get()
        {
            return todos.Values;
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public ActionResult<Todo> Get(Guid id)
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

        // POST api/values
        [HttpPost]
        public ActionResult<Todo> Post([FromBody] Todo value)
        {
            value.Id = Guid.NewGuid();
            todos.Add(value.Id.Value, value);
            return CreatedAtAction(nameof(Get), value);
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public ActionResult Put(Guid id, [FromBody] Todo value)
        {
            if (id != value.Id) return BadRequest();
            if (todos.TryGetValue(id, out var existing))
            {
                existing.Value = value.Value;
                return Ok();
            }
            else
            {
                return NotFound();
            }
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
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
