using GostObjectsClassLibrary.ProfileDoc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GRIF.Entities
{
    public class ExchangeDocumentDbData
    {
        public string Name { get; set; }
        public string NameWithAdditionalInfo { get; set; }
        public string profileTag { get; set; } = null!;
        public string creatorName { get; set; } = null!;
        public string modifierName { get; set; } = null!;
        public DateTime creationDate { get; set; }
        public DateTime modificationDate { get; set; }

        public ExchangeDocumentDbData(ProfileDoc profile)
        {
            this.profileTag = profile.profileTag;
            this.creatorName = profile.creatorName;
            this.modifierName = profile.modifierName;
            this.creationDate = profile.creationDate;
            this.modificationDate = profile.modificationDate;

            this.Name = profile.name;
            this.NameWithAdditionalInfo = $"{this.profileTag} (изм. \"{modifierName}\" от {modificationDate})";
        }
    }

}
