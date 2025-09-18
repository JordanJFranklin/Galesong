using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ink.Runtime;
using System.IO;
using FileManagement;


public class DialogueVariables
{
    public Dictionary<string, Ink.Runtime.Object> variables { get; private set; }
    private Story globalVariablesStory;
    private static string path = Application.dataPath + "/StoryProgression.JSON";

    public DialogueVariables(TextAsset loadGlobalsJSON)
    {
        // create the story
        globalVariablesStory = new Story(loadGlobalsJSON.text);

        //If saved data exists load it now
        PersistentStory.LoadStoryProgress(path, globalVariablesStory);

        // initialize the dictionary
        variables = new Dictionary<string, Ink.Runtime.Object>();
        foreach (string name in globalVariablesStory.variablesState)
        {
            Ink.Runtime.Object value = globalVariablesStory.variablesState.GetVariableWithName(name);
            variables.Add(name, value);
            Debug.Log("Initialized global dialogue variable: " + name + " = " + value);
        }
    }

    public void StartListening(Story story)
    {
        //Must be assigned before assigning a listener
        VariablesToStory(story);
        story.variablesState.variableChangedEvent += VariableChanged;
    }

    public void StopListening(Story story)
    {
        story.variablesState.variableChangedEvent -= VariableChanged;
    }

    private void VariableChanged(string name, Ink.Runtime.Object value)
    {
        //Maintain variables that are exclusively in the dictionary
        if (variables.ContainsKey(name))
        {
            variables.Remove(name);
            variables.Add(name, value);
            Debug.Log("Variable Changed: " + name + " = " + value);
        }
    }

    private void VariablesToStory(Story story)
    {
        foreach (KeyValuePair<string, Ink.Runtime.Object> variable in variables)
        {
            story.variablesState.SetGlobal(variable.Key, variable.Value);
        }
    }

    public void SaveGlobalVariables()
    {
        if(globalVariablesStory != null)
        {
            //Load current state of the global story
            VariablesToStory(globalVariablesStory);
            PersistentStory.SaveStoryProgress(path, globalVariablesStory);
            //Perform Your Write And Save Method Here
        }
    }
}