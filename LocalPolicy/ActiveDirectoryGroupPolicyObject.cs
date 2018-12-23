using System;
using System.Text;

namespace LocalPolicy
{
    public class ActiveDirectoryGroupPolicyObject : GroupPolicyObject
    {
        /// <summary>
        /// Opens the specified GPO as determined by the path
        /// </summary>
        /// <param name="activeDirectoryPath">
        /// Specifies the Active Directory path of the object to open.
        /// If the path specifies a domain controller, the GPO is created on that DC. 
        /// Otherwise, the system will select a DC on the caller's behalf.
        /// </param>
        public ActiveDirectoryGroupPolicyObject(string activeDirectoryPath, GroupPolicyObjectSettings settings = null)
        {
            settings = new GroupPolicyObjectSettings();
            trycatch(() => instance.OpenDSGPO(activeDirectoryPath, settings.Flag),
                "Unable to open GPO at ActiveDirectory path '{0}'", activeDirectoryPath);
        }
        private ActiveDirectoryGroupPolicyObject(COM.IGroupPolicyObject instance)
        {
            this.instance = instance;
        }

        /// <summary>
        /// Creates a new GPO in the Active Directory with the specified display name and opens
        /// it with the given settings.
        /// </summary>
        /// <param name="activeDirectoryPath">
        /// Specifies the Active Directory path of the object to create. 
        /// If the path specifies a domain controller, the GPO is created on that DC. 
        /// Otherwise, the system will select a DC on the caller's behalf.
        /// </param>
        /// <param name="displayName">Specifies the display name of the object to create.</param>
        public static ActiveDirectoryGroupPolicyObject Create(string activeDirectoryPath, string displayName,
            GroupPolicyObjectSettings settings = null)
        {
            settings = new GroupPolicyObjectSettings();
            var instance = getInstance();
            trycatch(() => instance.New(activeDirectoryPath, displayName, settings.Flag),
                "Unable to create new GPO instance with path '{0}' and display name '{1}'", 
                activeDirectoryPath, displayName);
            return new ActiveDirectoryGroupPolicyObject(instance);
        }

        public Guid GuidName
        {
            get
            {
                return new Guid(UniqueName);
            }
        }
        /// <summary>
        /// Retrieves the path to the root of the specified GPO section. 
        /// The path is in ADSI format (LDAP://cn=user, ou=users, dc=coname, dc=com).
        /// </summary>
        public override string GetPathTo(GroupPolicySection section)
        {
            StringBuilder sb = new StringBuilder(maxLength);
            trycatch(() => instance.GetDSPath((uint)section, sb, maxLength),
                "Unable to retrieve path to section '{0}'",
                Enum.GetName(typeof(GroupPolicySection), section));
            return sb.ToString();
        }
    }
}
