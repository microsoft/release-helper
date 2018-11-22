using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using System.Configuration;
using Newtonsoft.Json.Linq;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Contracts;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Clients;
using Microsoft.TeamFoundation.Core.WebApi;
using Newtonsoft.Json;

namespace Tapas.CICD.ReleaseHelper.Test
{
    public class TestInitialization : IDisposable
    {
        string pat = ConfigurationManager.AppSettings["pat"];
        string ProjectCollectionUrl = ConfigurationManager.AppSettings["ProjectCollectionUrl"];
        string ProjectName = ConfigurationManager.AppSettings["ProjectName"];
        string ReleaseDefinitionName = ConfigurationManager.AppSettings["ReleaseDefinitionName"];

        public TestInitialization()
        {
            TfsInfo info = new TfsInfo()
            {
                ProjectCollectionUrl = ProjectCollectionUrl,
                ProjectName = ProjectName,
                ReleaseDefinitionName = ReleaseDefinitionName
            };

            //Function to delete release environment
            ReleaseHelperTest.DeleteReleaseEnvironment(info, pat, "Envz_01 - Clone from Env1");

            //Function to delete release environment
            ReleaseHelperTest.DeleteReleaseEnvironment(info, pat, "Envz_8");
        }
        public void Dispose()
        {

        }
    }

    /// <summary>
    /// Not sure on naming convention, decided to go with format:
    /// MethodName_StateUnderTest_ExpectedBehavior
    /// </summary>

    public class ReleaseHelperTest : IClassFixture<TestInitialization>
    {
        string pat = ConfigurationManager.AppSettings["pat"];
        string ProjectCollectionUrl = ConfigurationManager.AppSettings["ProjectCollectionUrl"];
        string ProjectName = ConfigurationManager.AppSettings["ProjectName"];
        string ReleaseDefinitionName = ConfigurationManager.AppSettings["ReleaseDefinitionName"];
        string ReleaseDefinitionNameOneEnv = ConfigurationManager.AppSettings["ReleaseDefinitionNameOneEnv"];

        #region GetDeploymentErrors
        /// <summary>
        /// Test GetdeploymentErrors(int PastInstances)
        /// passing in the number of pastinstances set to 0
        /// so should always return no errors
        /// </summary>
        [Fact]
        public void GetDeploymentErrors_ZeroInstances_NoErrors()
        {
            TfsInfo info = new TfsInfo()
            {
                ProjectCollectionUrl = ProjectCollectionUrl,
                ProjectName = ProjectName,
                ReleaseDefinitionName = ReleaseDefinitionName
            };

            using (TfsRelease tfsRelease = new TfsRelease(info, pat))
            {
                string result = tfsRelease.GetDeploymentErrors(0);
                Assert.Equal($"**Warning** No errors found", result);
            };
        }

        [Theory]
        [InlineData(0)]
        public void GetDeploymentErrors_Data_NoErrors(int value)
        {
            TfsInfo info = new TfsInfo()
            {
                ProjectCollectionUrl = ProjectCollectionUrl,
                ProjectName = ProjectName,
                ReleaseDefinitionName = ReleaseDefinitionName
            };

            using (TfsRelease tfsRelease = new TfsRelease(info, pat))
            {
                string result = tfsRelease.GetDeploymentErrors(value);
                Assert.Equal($"**Warning** No errors found", result);
            };
        }

        /// <summary>
        /// Test GetdeploymentErrors(int PastInstances)
        /// passing in a fake release definition
        /// so should get no release definition error
        /// </summary>
        [Fact]
        public void GetDeploymentErrors_FakeRelease_NoReleaseError()
        {
            TfsInfo info = new TfsInfo()
            {
                ProjectCollectionUrl = ProjectCollectionUrl,
                ProjectName = ProjectName,
                ReleaseDefinitionName = "FakeRelease"
            };

            using (TfsRelease tfsRelease = new TfsRelease(info, pat))
            {
                string result = tfsRelease.GetDeploymentErrors(0);
                Assert.Equal($"**Warning** Failed to find Release Definition with name \"{info.ReleaseDefinitionName}\"", result);
            };
        }

        /// <summary>
        /// Test GetdeploymentErrors(int PastInstances)
        /// passing in a release definition that has 1 error
        /// so should get 1 release definition error
        /// </summary>
        [Fact]
        public void GetDeploymentErrors_ExistingError_OneError()
        {
            TfsInfo info = new TfsInfo()
            {
                ProjectCollectionUrl = ProjectCollectionUrl,
                ProjectName = ProjectName,
                ReleaseDefinitionName = ReleaseDefinitionName
            };

            using (TfsRelease tfsRelease = new TfsRelease(info, pat))
            {
                string result = tfsRelease.GetDeploymentErrors(1);
                dynamic obj = JArray.Parse(result);
                Assert.Equal(1, obj.Count);
            };
        }

        /// <summary>
        /// Test GetdeploymentErrors(DateTime mindate, DateTime maxdate)
        /// passing in a start date and end date where
        /// start date > end date so should always return no errors
        /// </summary>
        [Fact]
        public void GetDeploymentErrors_ImpossibleDates_NoErrors()
        {
            TfsInfo info = new TfsInfo()
            {
                ProjectCollectionUrl = ProjectCollectionUrl,
                ProjectName = ProjectName,
                ReleaseDefinitionName = ReleaseDefinitionName
            };

            using (TfsRelease tfsRelease = new TfsRelease(info, pat))
            {
                DateTime dtstart = new DateTime(2018, 10, 10);
                DateTime dtend = new DateTime(2018, 9, 10);
                string result = tfsRelease.GetDeploymentErrors(dtstart, dtend);
                Assert.Equal($"**Warning** No errors found", result);
            };
        }

        /// <summary>
        /// Test GetdeploymentErrors(DateTime mindate, DateTime maxdate)
        /// passing in a start date and end date where
        /// there exists 1 error so should always return 1 error
        /// </summary>
        [Fact]
        public void GetDeploymentErrors_ValidDateRangeWithOne_OneError()
        {
            TfsInfo info = new TfsInfo()
            {
                ProjectCollectionUrl = ProjectCollectionUrl,
                ProjectName = ProjectName,
                ReleaseDefinitionName = ReleaseDefinitionName
            };

            using (TfsRelease tfsRelease = new TfsRelease(info, pat))
            {
                DateTime dtstart = new DateTime(2018, 10, 31);
                DateTime dtend = new DateTime(2018, 11, 2);
                string result = tfsRelease.GetDeploymentErrors(dtstart, dtend);
                dynamic obj = JArray.Parse(result);
                Assert.Equal(1, obj.Count);
            };
        }
        #endregion //GetDeploymentErrors


        #region GetTfsReleaseEnvironmentNames

        /// <summary>
        /// Test GetTfsReleaseEnvironmentNames()
        /// passing in a non existent release definition name
        /// so an empty list should be returned
        /// </summary>
        [Fact]
        public void GetTfsReleaseEnvironmentNames_FakeRelease_ErrorMessage()
        {
            TfsInfo info = new TfsInfo()
            {
                ProjectCollectionUrl = ProjectCollectionUrl,
                ProjectName = ProjectName,
                ReleaseDefinitionName = "FakeRelease"
            };

            using (TfsRelease tfsRelease = new TfsRelease(info, pat))
            {
                string result = tfsRelease.GetTfsReleaseEnvironmentNames();
                Assert.Equal($"**Warning** Failed to find Release Definition with name \"{info.ReleaseDefinitionName}\"", result);
            };
        }

        /// <summary>
        /// Test GetTfsReleaseEnvironmentNames()
        /// passing in a test release definition name
        /// with a result of one item returned
        /// </summary>
        [Fact]
        public void GetTfsReleaseEnvironmentNames_TestRelease_OneItem()
        {
            TfsInfo info = new TfsInfo()
            {
                ProjectCollectionUrl = ProjectCollectionUrl,
                ProjectName = ProjectName,
                ReleaseDefinitionName = ReleaseDefinitionNameOneEnv
            };

            using (TfsRelease tfsRelease = new TfsRelease(info, pat))
            {
                string result = tfsRelease.GetTfsReleaseEnvironmentNames();
                dynamic obj = JObject.Parse(result);
                Assert.Equal(1,obj.EnvironmentNames.Count);
            };
        }
        #endregion //GetTfsReleaseEnvironmentNames


        #region CloneReleaseEnvironment

        /// <summary>
        /// Test CloneReleaseEnvironment()
        /// passing in a fake release definition name
        /// with a result of an error message
        /// </summary>
        [Fact]
        public void CloneReleaseEnvironment_FakeRelease_ErrorMessage()
        {
            TfsInfo info = new TfsInfo()
            {
                ProjectCollectionUrl = ProjectCollectionUrl,
                ProjectName = ProjectName,
                ReleaseDefinitionName = "FakeRelease"
            };

            using (TfsRelease tfsRelease = new TfsRelease(info, pat))
            {
                string result = tfsRelease.CloneReleaseEnvironment("DoesntMatter0","DoesntMatter1");
                Assert.Equal($"**Warning** Failed to find Release Definition with name \"{info.ReleaseDefinitionName}\"", result);
            };
        }

        /// <summary>
        /// Test CloneReleaseEnvironment()
        /// passing in a fake source env name
        /// with a result of an error message
        /// </summary>
        [Fact]
        public void CloneReleaseEnvironment_FakeEnv_ErrorMessage()
        {
            TfsInfo info = new TfsInfo()
            {
                ProjectCollectionUrl = ProjectCollectionUrl,
                ProjectName = ProjectName,
                ReleaseDefinitionName = ReleaseDefinitionName
            };

            using (TfsRelease tfsRelease = new TfsRelease(info, pat))
            {
                string result = tfsRelease.CloneReleaseEnvironment("FakeEnv", "DoesntMatter1");
                Assert.Equal($"**Warning** Failed to find Env with name \"FakeEnv\"", result);
            };
        }

        /// <summary>
        /// Test CloneReleaseEnvironment()
        /// passing in an existing target env name
        /// with a result of an error message
        /// </summary>
        [Fact]
        public void CloneReleaseEnvironment_ExistingTargetEnv_ErrorMessage()
        {
            TfsInfo info = new TfsInfo()
            {
                ProjectCollectionUrl = ProjectCollectionUrl,
                ProjectName = ProjectName,
                ReleaseDefinitionName = ReleaseDefinitionName
            };

            using (TfsRelease tfsRelease = new TfsRelease(info, pat))
            {
                string result = tfsRelease.CloneReleaseEnvironment("Env1", "Env0");
                Assert.Equal($"**Warning** An Env with name \"Env0\" already exists", result);
            };
        }

        /// <summary>
        /// Test CloneReleaseEnvironment()
        /// passing in valid input
        /// with a result of a success
        /// PROBABLY WANT TO HAVE DELETE ENV AFTER RUN
        /// </summary>
        [Fact]
        public void CloneReleaseEnvironment_ValidInput_Success()
        {
            TfsInfo info = new TfsInfo()
            {
                ProjectCollectionUrl = ProjectCollectionUrl,
                ProjectName = ProjectName,
                ReleaseDefinitionName = ReleaseDefinitionName
            };

            using (TfsRelease tfsRelease = new TfsRelease(info, pat))
            {
                string result = tfsRelease.CloneReleaseEnvironment("Env1", "Envz_8");
                dynamic obj = JObject.Parse(result);
                Assert.True((bool)obj.success);
            };
        }

        /// <summary>
        /// Test CloneReleaseEnvironment()
        /// passing in a fake release definition name
        /// with a result of an error message
        /// </summary>
        [Fact]
        public void CloneReleaseEnvironmentWithId_FakeRelease_ErrorMessage()
        {
            TfsInfo info = new TfsInfo()
            {
                ProjectCollectionUrl = ProjectCollectionUrl,
                ProjectName = ProjectName,
                ReleaseDefinitionName = "FakeRelease"
            };

            using (TfsRelease tfsRelease = new TfsRelease(info, pat))
            {
                string result = tfsRelease.CloneReleaseEnvironment("DoesntMatter0", "DoesntMatter1", 0);
                Assert.Equal($"**Warning** Failed to find Release Definition with name \"{info.ReleaseDefinitionName}\"", result);
            };
        }

        /// <summary>
        /// Test CloneReleaseEnvironment()
        /// passing in a fake source env name
        /// with a result of an error message
        /// </summary>
        [Fact]
        public void CloneReleaseEnvironmentWithId_FakeEnv_ErrorMessage()
        {
            TfsInfo info = new TfsInfo()
            {
                ProjectCollectionUrl = ProjectCollectionUrl,
                ProjectName = ProjectName,
                ReleaseDefinitionName = ReleaseDefinitionName
            };

            using (TfsRelease tfsRelease = new TfsRelease(info, pat))
            {
                string result = tfsRelease.CloneReleaseEnvironment("FakeEnv", "DoesntMatter1", 0);
                Assert.Equal($"**Warning** Failed to find Env with name \"FakeEnv\"", result);
            };
        }

        /// <summary>
        /// Test CloneReleaseEnvironment()
        /// passing in an existing target env name
        /// with a result of an error message
        /// </summary>
        [Fact]
        public void CloneReleaseEnvironmentWithId_ExistingTargetEnv_ErrorMessage()
        {
            TfsInfo info = new TfsInfo()
            {
                ProjectCollectionUrl = ProjectCollectionUrl,
                ProjectName = ProjectName,
                ReleaseDefinitionName = ReleaseDefinitionName
            };

            using (TfsRelease tfsRelease = new TfsRelease(info, pat))
            {
                string result = tfsRelease.CloneReleaseEnvironment("Env1", "Env0", 0);
                Assert.Equal($"**Warning** An Env with name \"Env0\" already exists", result);
            };
        }

        /// <summary>
        /// Test CloneReleaseEnvironment()
        /// passing in valid input
        /// with a result of a success
        /// PROBABLY WANT TO HAVE DELETE ENV AFTER RUN
        /// </summary>
        [Fact]
        public void CloneReleaseEnvironmentWithId_ValidInput_Success()
        {
            TfsInfo info = new TfsInfo()
            {
                ProjectCollectionUrl = ProjectCollectionUrl,
                ProjectName = ProjectName,
                ReleaseDefinitionName = ReleaseDefinitionName
            };

            using (TfsRelease tfsRelease = new TfsRelease(info, pat))
            {
                string result = tfsRelease.CloneReleaseEnvironment("Env1", "Envz_01", 0);
                dynamic obj = JObject.Parse(result);
                Assert.True((bool)obj.success);
            };
        }

        #endregion //CloneReleaseEnvironment


        public static string DeleteReleaseEnvironment(TfsInfo info, string pat, string SourceEnvName)
        {

            using (TfsRelease tfsRelease = new TfsRelease(info, pat))
            {
                VssConnection connection = new VssConnection(new Uri(info.ProjectCollectionUrl), new VssBasicCredential(string.Empty, pat));
                var relclient = connection.GetClient<ReleaseHttpClient>();
                var projclient = connection.GetClient<ProjectHttpClient>();

                string result = "";

                var definitions = relclient.GetReleaseDefinitionsAsync(info.ProjectName, info.ReleaseDefinitionName, ReleaseDefinitionExpands.Environments, isExactNameMatch: true).Result;

                if (definitions.Count() > 0)
                {
                    //This call is to get the project GUID
                    var project = projclient.GetProject(info.ProjectName).Result;
                    //This returns everything in a definition including properties such as Owner in an Environment
                    var def = relclient.GetReleaseDefinitionAsync(project.Id, definitions.First().Id).Result;

                    var envcount = def.Environments.Count;

                    var defenvlist = def.Environments.Where(e => e.Name == SourceEnvName);
                    if (defenvlist.Count() > 0)
                    {
                        var source = defenvlist.First();

                        foreach (var env in def.Environments)
                        {
                            if (env.Rank > source.Rank)
                                env.Rank--;
                        }

                        def.Environments.Remove(source);

                        var newdef = relclient.UpdateReleaseDefinitionAsync(def, project.Id).Result;

                        var envinfo = new TfsCloneReleaseEnvInfo()
                        {
                            ReleaseName = newdef.Name,
                            EnvironmentName = source.Name,
                            RetentionPolicy = source.RetentionPolicy.ToString(),
                            Rank = source.Rank,
                            Owner = source.Owner.DisplayName,
                            PreDeployApprovals = source.PreDeployApprovals.Approvals.Where(e => e.Approver != null).OrderBy(e => e.Approver.DisplayName).Select(e => e.Approver.DisplayName).ToArray(),
                            PostDeployApprovals = source.PostDeployApprovals.Approvals.Where(e => e.Approver != null).OrderBy(e => e.Approver.DisplayName).Select(e => e.Approver.DisplayName).ToArray(),
                            DeployPhases = source.DeployPhases.OrderBy(e => e.Name).Select(e => e.Name).ToArray(),
                            success = (newdef.Environments.Count < envcount) ? true : false //Basic detection if environment was added
                        };

                        result = JsonConvert.SerializeObject(envinfo, Formatting.Indented);

                    }
                    else { result = $"**Warning** Failed to find Env with name \"{SourceEnvName}\""; }
                }
                else { result = $"**Warning** Failed to find Release Definition with name \"{info.ReleaseDefinitionName}\""; }

                return result;
            }
        }
    }
}
