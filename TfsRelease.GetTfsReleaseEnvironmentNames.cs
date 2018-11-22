using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Contracts;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Tapas.CICD.ReleaseHelper
{
    public partial class TfsRelease
    {
        public string GetTfsReleaseEnvironmentNames()
        {
            string result = "";
            var definitions = relclient.GetReleaseDefinitionsAsync(TfsEnvInfo.ProjectName, TfsEnvInfo.ReleaseDefinitionName, ReleaseDefinitionExpands.Environments, isExactNameMatch: true).Result;
            if (definitions.Count() > 0)
            {
                var def = definitions.First();

                var releaseinfo = new TfsReleaseInfo()
                {
                    ReleaseName = def.Name,
                    ReleaseNameFormat = def.Name,
                    Comment = def.Comment,
                    IsDeleted = def.IsDeleted,
                    ModifiedOn = def.ModifiedOn,
                    ModifiedBy = def.ModifiedBy.DisplayName,
                    CreatedOn = def.CreatedOn,
                    CreatedBy = def.CreatedBy.DisplayName,
                    Description = def.Description,
                    Revision = def.Revision,
                    EnvironmentNames = def.Environments.OrderBy(e => e.Name).Select(e => e.Name).ToArray()
                };

                result = JsonConvert.SerializeObject(releaseinfo, Formatting.Indented);
            }
            else { result = $"**Warning** Failed to find Release Definition with name \"{TfsEnvInfo.ReleaseDefinitionName}\""; }

            return result;
        }
    }
}
