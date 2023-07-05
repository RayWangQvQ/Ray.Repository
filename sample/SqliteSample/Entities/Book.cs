using System.ComponentModel.DataAnnotations.Schema;
using Ray.DDD;

namespace SqliteSample.Entities
{
    public class Book:IEntity<long>
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public string Title { get; set; }

        public string Author { get; set; }
    }
}
