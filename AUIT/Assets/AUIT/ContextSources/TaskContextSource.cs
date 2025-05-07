using UnityEngine;

public class TaskContextSource : ContextSource<TaskContextSource.Task>
{
    public enum Task
    {
        NONE,
        COOKING,
        LAUNDRY,
        PRODUCTIVITY
    }

    public Task task;

    public override TaskContextSource.Task GetValue()
    {
        return task;
    }
}
