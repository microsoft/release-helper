using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Clients;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Net;

namespace Tapas.CICD.ReleaseHelper
{
    //Sample output
    //release name: Release_20181017.1
    //phase type: PhaseDeployment
    //attempt #: 1
    //agent name: BUILD_Agent1
    //task name: Build Solution
    //task starttime: 10/17/2018 10:22:20 PM
    //task endtime: 10/17/2018 10:22:32 PM
    //errors messages:
    //      Issues[0].Message
    //      Issues[1].Message
    //      Issues[2].Message

    public struct TfsReleaseError
    {
        public string ReleaseName;
        public string EnvironmentName;
        public string PhaseType;
        public int Attempt;
        public string AgentName;
        public string TaskName;
        public DateTime? StartTime;
        public DateTime? FinishTime;
        public string[] ErrorMessages;
        public string ErrorLog;
        public Dictionary<string, string> TaskInputs;
        public TfsArtifact[] Artifacts;
    }

    public struct TfsArtifact
    {
        public string Build;
        public string BuildUrl;
    }

    public struct TfsInfo
    {
        public string ProjectCollectionUrl;
        public string ProjectName;
        public string ReleaseDefinitionName;
        public int ReleaseDefinitionID;
        public string ReleaseName;
        public string EnvironmentName;
    }

    public struct TfsCloneReleaseEnvInfo
    {
        public string ReleaseName;
        public string EnvironmentName;
        public string CloneFrom;
        public string RetentionPolicy;
        public int Rank;
        public string Owner;
        public string[] PreDeployApprovals;
        public string[] PostDeployApprovals;
        public string[] DeployPhases;
        public bool success;
    }

    public struct TfsReleaseInfo
    {
        public string ReleaseName;
        public string Comment;
        public string ReleaseNameFormat;
        public bool IsDeleted;
        public DateTime ModifiedOn;
        public string ModifiedBy;
        public DateTime CreatedOn;
        public string CreatedBy;
        public string Description;
        public int Revision;
        public string[] EnvironmentNames;
    }

    public partial class TfsRelease : IDisposable
    {
        // Dispose pattern. Refer https://msdn.microsoft.com/en-us/library/system.idisposable(v=vs.110).aspx
        // Track whether Dispose has been called.
        private bool disposed = false;

        private WebClient client;
        private ReleaseHttpClient relclient;
        private ProjectHttpClient projclient;

        public TfsInfo TfsEnvInfo { get; internal set; }

        public TfsRelease(TfsInfo TfsEnvInfo)
        {
            this.TfsEnvInfo = TfsEnvInfo;

            // Interactively ask the user for credentials, caching them so the user isn't constantly prompted
            VssCredentials credentials = new VssClientCredentials();
            credentials.Storage = new VssClientCredentialStorage();

            VssConnection connection = new VssConnection(new Uri(this.TfsEnvInfo.ProjectCollectionUrl), credentials);
            relclient = connection.GetClient<ReleaseHttpClient>();
            projclient = connection.GetClient<ProjectHttpClient>();

            client = new WebClient();
            client.Credentials = credentials.Windows.Credentials;

        }

        public TfsRelease(TfsInfo TfsEnvInfo, string pat)
        {
            this.TfsEnvInfo = TfsEnvInfo;

            // Use PAT in order to perform rest calls
            VssConnection connection = new VssConnection(new Uri(this.TfsEnvInfo.ProjectCollectionUrl), new VssBasicCredential(string.Empty, pat));
            relclient = connection.GetClient<ReleaseHttpClient>();
            projclient = connection.GetClient<ProjectHttpClient>();
        }

        // Implement IDisposable.
        // Do not make this method virtual.
        // A derived class should not be able to override this method.
        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        // Dispose(bool disposing) executes in two distinct scenarios.
        // If disposing equals true, the method has been called directly
        // or indirectly by a user's code. Managed and unmanaged resources
        // can be disposed.
        // If disposing equals false, the method has been called by the
        // runtime from inside the finalizer and you should not reference
        // other objects. Only unmanaged resources can be disposed.
        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose resources.
                }

                // Note disposing has been done.
                disposed = true;

            }
        }
        // Use C# destructor syntax for finalization code.
        // This destructor will run only if the Dispose method
        // does not get called.
        // It gives your base class the opportunity to finalize.
        // Do not provide destructors in types derived from this class.
        ~TfsRelease()
        {
            // Do not re-create Dispose clean-up code here.
            // Calling Dispose(false) is optimal in terms of
            // readability and maintainability.
            Dispose(false);
        }
    }
}
