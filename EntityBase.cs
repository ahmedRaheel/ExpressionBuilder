using System;
using System.ComponentModel.DataAnnotations.Schema;
using IMatch.Common.Interface;
namespace IMatch.DataAccess.Base
{
    public class EntityBase : IEntity
    {
        public int ID { get; set; }
        //public TimeSpan Version { get; set; }
        [Column(TypeName = "datetime2"), DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime ValidFrom { get; set; }
        [Column(TypeName = "datetime2"), DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime ValidTo { get; set; }
    }
}
