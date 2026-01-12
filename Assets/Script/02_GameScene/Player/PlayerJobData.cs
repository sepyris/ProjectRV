using System;
using UnityEditor.U2D.Animation;

[Serializable]
public class PlayerJobData
{
    public JobsType jobType;
    public bool isCurrentJob;

    public PlayerJobData(JobsType jobType, bool isCurrentJob)
    {
        this.jobType = jobType;
        this.isCurrentJob = isCurrentJob;
    }

    public string GetJobName()
    {
        if (JobsDataManager.Instance != null)
        {
            JobsData data = JobsDataManager.Instance.GetJobDataByType(jobType);
            return data != null ? data.jobName : jobType.ToString();
        }
        return jobType.ToString();
    }
}

[Serializable]
public class PlayerJobSaveData
{
    public int jobType;
    public bool isCurrentJob;
}