﻿using Ray.DDD;
using System.ComponentModel.DataAnnotations.Schema;

namespace SqlServerSample.Entities
{
    public class Book:IEntity<long>
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public string Title { get; set; }

        public string Author { get; set; }
    }
}
