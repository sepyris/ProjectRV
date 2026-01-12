using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class JobsDataManager : MonoBehaviour
{
    public static JobsDataManager Instance { get; private set; }

    [Header("SO Data")]
    public JobsDataSO jobDatabaseSO;

    private readonly Dictionary<string, JobsData> jobDatabaseById = new();
    private readonly Dictionary<JobsType, JobsData> jobDatabaseByType = new();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (jobDatabaseSO != null)
            {
                BuildDictionary(jobDatabaseSO);
            }
            else
            {
                Debug.LogWarning("[JobsDataManager] SO data is not assigned.");
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void BuildDictionary(JobsDataSO database)
    {
        jobDatabaseById.Clear();
        jobDatabaseByType.Clear();

        foreach (var item in database.Items)
        {
            if (!jobDatabaseById.ContainsKey(item.jobId))
            {
                jobDatabaseById.Add(item.jobId, item);
            }

            if (!jobDatabaseByType.ContainsKey(item.jobstype))
            {
                jobDatabaseByType.Add(item.jobstype, item);
            }
        }
    }

    public JobsData GetJobData(string jobId)
    {
        if (jobDatabaseById.TryGetValue(jobId, out JobsData data))
        {
            return data;
        }
        return null;
    }

    public JobsData GetJobDataByType(JobsType jobType)
    {
        if (jobDatabaseByType.TryGetValue(jobType, out JobsData data))
        {
            return data;
        }
        return null;
    }

    public List<JobsData> GetAllJobs()
    {
        return jobDatabaseById.Values.ToList();
    }
}