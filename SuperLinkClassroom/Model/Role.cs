using System;

namespace SuperLinkClassroom.Model
{
    public class Role
    {
        public Guid Id { get; set; }
        public string RoleName { get; set; }

        public Role(Guid id, string roleName)
        {
            Id = id;
            RoleName = roleName;
        }
    }
}