using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Ray.Repository;
using SqliteSample.Entities;

namespace SqlServerSample.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookController : ControllerBase
    {
        private readonly IBaseRepository<Book, long> _repo;

        public BookController(IBaseRepository<Book,long> repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public async Task<List<Book>> GetList()
        {
            return await _repo.GetAllListAsync();
        }

        [HttpPost]
        public async Task Add(Book request)
        {
            request.Id = 0;
            await _repo.InsertAsync(request, true);
        }
    }
}
