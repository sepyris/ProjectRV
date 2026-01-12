using System;
using System.Collections.Generic;
using UnityEngine;

public class JobsDataSO : ScriptableObject
{
    public List<JobsData> Items = new List<JobsData>();
}

[System.Serializable]
public class JobsData
{
    public string jobId;
    public string jobName;
    public JobsType jobstype;
    public string description;

    public int requiredLevel;

    public string jobIconPath;
}

public enum JobsType
{
    None = 0,
    Novice,
    Warrior,
    Mage
}