using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperLinkClassroom.Model
{
    public class User
    {
        public Guid Id { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string UserIP { get; set; }
        public Guid RoleId { get; set; }
    
        // public ICollection<AttendanceLine> AttendanceLines { get; set; }
        public User(Guid id, string userName, string password, string userIp, Guid roleId)
        {
            Id = id;
            UserName = userName;
            Password = password;
            UserIP = userIp;
            RoleId = roleId;
        }
    }
}

