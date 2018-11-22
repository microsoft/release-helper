using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Contracts;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.IO;
using System;

namespace Tapas.CICD.ReleaseHelper
{
    public partial class TfsRelease
    {
        public string GetDeploymentErrors(int PastInstances)
        {
            string result = "";
            List<TfsReleaseError> errorlist = new List<TfsReleaseError>();
            Dictionary<string, string> inputs = new Dictionary<string, string>();

            var def = relclient.GetReleaseDefinitionsAsync(TfsEnvInfo.ProjectName, TfsEnvInfo.ReleaseDefinitionName, isExactNameMatch: true).Result;

            if (def.Count() > 0)
            {
                var id = def.First().Id;

                var runs = relclient.GetDeploymentsAsync(project: TfsEnvInfo.ProjectName, definitionId: id, deploymentStatus: DeploymentStatus.Failed, operationStatus: DeploymentOperationStatus.PhaseFailed, queryOrder: ReleaseQueryOrder.Descending, top: PastInstances).Result;

                foreach (var run in runs)
                {
                    var rel = relclient.GetReleaseAsync(TfsEnvInfo.ProjectName, run.Release.Id).Result;
                    var env = rel.Environments.First(e => e.Id == run.ReleaseEnvironmentReference.Id);
                    var attempt = env.DeploySteps.First(s => s.Attempt == run.Attempt);
                    var phase = attempt.ReleaseDeployPhases.First(p => p.Status == DeployPhaseStatus.Failed);

                    //assumption here is each phase has only one job that failed
                    var job = phase.DeploymentJobs.First(j => j.Job.Status == Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.TaskStatus.Failed);
                    var failedtask = job.Tasks.Where(t => t.Status == Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.TaskStatus.Failed).First();

                    var error = new TfsReleaseError()
                    {
                        ReleaseName = rel.Name,
                        EnvironmentName = env.Name,
                        Attempt = attempt.Attempt,
                        PhaseType = phase.PhaseType.ToString(),
                        AgentName = failedtask.AgentName,
                        TaskName = failedtask.Name,
                        StartTime = failedtask.StartTime,
                        FinishTime = failedtask.FinishTime,
                        ErrorMessages = failedtask.Issues.Where(i => i.Message != "").Select(i => i.Message).ToArray()
                    };

                    var snapshot = env.DeployPhasesSnapshot.First(dps => dps.Rank == phase.Rank);
                    if (failedtask.Task != null)    //failing at download artifacts
                    {
                        inputs = snapshot.WorkflowTasks.First(w => w.TaskId == failedtask.Task.Id).Inputs;
                    }

                    error.TaskInputs = inputs;

                    List<TfsArtifact> artifacts = new List<TfsArtifact>();
                    foreach (var a in rel.Artifacts)
                    {
                        var build = a.DefinitionReference.Where(d => d.Key == "version").Select(v => v.Value.Name).First();
                        var buildurl = a.DefinitionReference.Where(d => d.Key == "artifactSourceVersionUrl").Select(v => v.Value.Id).First();
                        artifacts.Add(new TfsArtifact()
                        {
                            Build = build,
                            BuildUrl = buildurl
                        });
                    }
                    error.Artifacts = artifacts.ToArray();

                    errorlist.Add(error);
                }

                //To download all logs for a given release
                //docs: https://docs.microsoft.com/en-us/rest/api/vsts/release/releases/get%20logs?view=vsts-rest-4.1
                //GET https://{accountName}.vsrm.visualstudio.com/{project}/_apis/release/releases/{releaseId}/logs?api-version=4.1-preview.2
                //client.DownloadFile(rel.LogsContainerUrl, rel.Name + ".zip");
                //The return data seems to have bad encoding; looks like a bug
                //var logs = tfsclient.GetLogsAsync(tfsinfo.ProjectName, id).Result;

                //Handle case when no errors are found
                if (errorlist.Count > 0)
                {
                    result = JsonConvert.SerializeObject(errorlist, Formatting.Indented);
                }
                else { result = $"**Warning** No errors found"; }
            }
            else { result = $"**Warning** Failed to find Release Definition with name \"{TfsEnvInfo.ReleaseDefinitionName}\""; }

            return result;
        }

        public string GetLatestError()
        {
            string result = "";
            List<TfsReleaseError> errorlist = new List<TfsReleaseError>();

            var deflist = relclient.GetReleaseDefinitionsAsync(TfsEnvInfo.ProjectName, TfsEnvInfo.ReleaseDefinitionName, isExactNameMatch: true, expand: ReleaseDefinitionExpands.Environments).Result;

            if (deflist.Count() > 0)
            {
                var def = deflist.First();
                var defenvlist = def.Environments.Where(e => e.Name == TfsEnvInfo.EnvironmentName);

                if (defenvlist.Count() > 0)
                {
                    var defenv = defenvlist.First();
                    var rellist = relclient.GetReleasesAsync(def.Id, defenv.Id).Result;
                    
                    if (rellist.Count() > 0)
                    {
                        var rel = rellist.Where(r => r.Name == TfsEnvInfo.ReleaseName);

                        if (rel.Count() > 0)
                        {
                            var relid = rel.First().Id;
                            var reldetails = relclient.GetReleaseAsync(TfsEnvInfo.ProjectName, relid).Result;
                            var env = reldetails.Environments.Where(e => e.Name == TfsEnvInfo.EnvironmentName).First();
                            var steplist = env.DeploySteps.Where(s => s.Status == DeploymentStatus.Failed);

                            foreach (var step in steplist)
                            {
                                //assuming only one phase that fails in any one deployment
                                var phase = step.ReleaseDeployPhases.Where(p => p.Status == DeployPhaseStatus.Failed).First();
                                //assumption here is each phase has only one job that failed
                                var job = phase.DeploymentJobs.First(j => j.Job.Status == TaskStatus.Failed);
                                var failedtask = job.Tasks.Where(t => t.Status == TaskStatus.Failed).First();
                                var error = new TfsReleaseError()
                                {
                                    ReleaseName = reldetails.Name,
                                    EnvironmentName = env.Name,
                                    Attempt = step.Attempt,
                                    PhaseType = phase.PhaseType.ToString(),
                                    AgentName = failedtask.AgentName,
                                    TaskName = failedtask.Name,
                                    StartTime = failedtask.StartTime,
                                    FinishTime = failedtask.FinishTime,
                                    ErrorMessages = failedtask.Issues.Where(i => i.Message != "").Select(i => i.Message).ToArray()
                                };

                                var logstream = relclient.GetTaskLogAsync(TfsEnvInfo.ProjectName, reldetails.Id, env.Id, phase.Id, failedtask.Id).Result;
                                var reader = new StreamReader(logstream);
                                var logstring = reader.ReadToEnd();

                                var lines = logstring.Replace("\n", "").Split('\r');

                                var errorindex = Array.FindIndex(lines, l => l.Contains(error.ErrorMessages.First()));
                                string logsample = "";
                                var samplelength = 10;
                                int begin = (errorindex - samplelength) > 0 ? (errorindex - samplelength) : 0;
                                int end = (errorindex + samplelength) < lines.Length ? (errorindex + samplelength) : lines.Length;
                                for (int i = begin; i < end; i++)
                                {
                                    logsample += lines[i];
                                }

                                error.ErrorLog = logstring;
                                errorlist.Add(error);
                            }

                            //Handle case when no errors are found
                            if (errorlist.Count > 0)
                            {
                                result = JsonConvert.SerializeObject(errorlist, Formatting.Indented);
                            }
                            else { result = $"**Warning** No errors found"; }
                        }
                        else { result = $"**Warning** Failed to find Release Details with name \"{TfsEnvInfo.ReleaseName}\""; }
                    }
                    else { result = $"**Warning** Failed to find Release with name \"{TfsEnvInfo.ReleaseName}\""; }
                }
                else { result = $"**Warning** Failed to find Env with name \"{TfsEnvInfo.EnvironmentName}\""; }
            }
            else { result = $"**Warning** Failed to find Release Definition with name \"{TfsEnvInfo.ReleaseDefinitionName}\""; }

            return result;
        }

        public string GetDeploymentErrors(DateTime mindate, DateTime maxdate)
        {
            string result = "";
            List<TfsReleaseError> errorlist = new List<TfsReleaseError>();
            Dictionary<string, string> inputs = new Dictionary<string, string>();

            var runs = relclient.GetDeploymentsAsync(project: TfsEnvInfo.ProjectName, minStartedTime: mindate, maxStartedTime: maxdate, deploymentStatus: DeploymentStatus.Failed, operationStatus: DeploymentOperationStatus.PhaseFailed, queryOrder: ReleaseQueryOrder.Descending).Result;

            foreach (var run in runs)
            {
                var rel = relclient.GetReleaseAsync(TfsEnvInfo.ProjectName, run.Release.Id).Result;
                var env = rel.Environments.First(e => e.Id == run.ReleaseEnvironmentReference.Id);
                var attempt = env.DeploySteps.First(s => s.Attempt == run.Attempt);
                var phase = attempt.ReleaseDeployPhases.First(p => p.Status == DeployPhaseStatus.Failed);

                //assumption here is each phase has only one job that failed
                var job = phase.DeploymentJobs.First(j => j.Job.Status == TaskStatus.Failed);
                var failedtask = job.Tasks.Where(t => t.Status == TaskStatus.Failed).First();

                //GetDeploymentsAsync return top 50 regardless of minStartedTime or maxStartedTime
                //looks like a bug 
                if (failedtask.StartTime >= mindate && failedtask.StartTime <= maxdate)
                {
                    var error = new TfsReleaseError()
                    {
                        ReleaseName = rel.Name,
                        EnvironmentName = env.Name,
                        Attempt = attempt.Attempt,
                        PhaseType = phase.PhaseType.ToString(),
                        AgentName = failedtask.AgentName,
                        TaskName = failedtask.Name,
                        StartTime = failedtask.StartTime,
                        FinishTime = failedtask.FinishTime,
                        ErrorMessages = failedtask.Issues.Where(i => i.Message != "").Select(i => i.Message).ToArray()
                    };

                    var snapshot = env.DeployPhasesSnapshot.First(dps => dps.Rank == phase.Rank);
                    if (failedtask.Task != null)    //failing at download artifacts
                    {
                        inputs = snapshot.WorkflowTasks.First(w => w.TaskId == failedtask.Task.Id).Inputs;
                    }

                    error.TaskInputs = inputs;

                    List<TfsArtifact> artifacts = new List<TfsArtifact>();
                    foreach (var a in rel.Artifacts)
                    {
                        var build = a.DefinitionReference.Where(d => d.Key == "version").Select(v => v.Value.Name).First();
                        var q = a.DefinitionReference.Where(d => d.Key == "artifactSourceVersionUrl").Select(v => v.Value.Id);
                        var buildurl = (q.Count() > 0) ? q.First() : "";
                        artifacts.Add(new TfsArtifact()
                        {
                            Build = build,
                            BuildUrl = buildurl
                        });
                    }
                    error.Artifacts = artifacts.ToArray();

                    errorlist.Add(error);
                }

            }

            //Handle case when no errors are found
            if (errorlist.Count > 0)
            {
                result = JsonConvert.SerializeObject(errorlist, Formatting.Indented);
            }
            else { result = $"**Warning** No errors found"; }

            return result;
        }
    }
}
