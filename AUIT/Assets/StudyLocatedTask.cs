using UnityEngine;

public class StudyLocatedTask
{
    public (int, int)[] taskKeyIndices;
    public int targetKeyIndex;

    public int targetBox;

    public StudyLocatedTask((int, int)[] taskKeyIndices, int targetKeyIndex, int targetBox)
    {
        this.taskKeyIndices = taskKeyIndices;
        this.targetKeyIndex = targetKeyIndex;
        this.targetBox = targetBox;
    }
}