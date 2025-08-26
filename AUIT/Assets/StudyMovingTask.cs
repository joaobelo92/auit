using UnityEngine;

public class StudyNonLocatedTask
{
}

public class StudyPromptTask : StudyNonLocatedTask
{
    public Color color;

    public StudyPromptTask(Color color)
    {
        this.color = color;
    }
}

public class StudyVisualAttentionTask : StudyNonLocatedTask
{
    public int faces; // 1- sphere; 4 faces - tetrahedron; 5 - square pyramid; 6 - cube; 8 - octahedron

    public int correctColorFaces; // Number of faces that should be the same color

    public int[] possibleAnswers;

    public StudyVisualAttentionTask(int faces, int correctColorFaces, int[] possibleAnswers)
    {
        if (faces == 1 || faces == 4 || faces == 5 || faces == 6 || faces == 8)
        {
            this.faces = faces;
        }
        else
        {
            throw new System.ArgumentException("Invalid number of faces for the task.");
        }

        if (correctColorFaces > faces)
        {
            throw new System.ArgumentException("Invalid number of correct color faces for the task.");
        }

        if (possibleAnswers == null || possibleAnswers.Length != 3)
        {
            throw new System.ArgumentException("Possible answers cannot be null and must have 3 options.");
        }

        this.correctColorFaces = correctColorFaces;
        this.possibleAnswers = possibleAnswers;
    }
}