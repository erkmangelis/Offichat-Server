using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Offichat.Domain.Entities
{
    public class Role
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Name { get; set; } // "Admin", "User"

        // Navigation Property: Bu role sahip kullanıcıları görmek için (ICollection mantıklı)
        public ICollection<User> Users { get; set; }
    }
}
