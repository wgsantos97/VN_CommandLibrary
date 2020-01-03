# Optimizing the Novel Controller

## Intro

Over the summer, I followed a YouTube series about the creation of a Unity Visual Novel System. I wanted to integrate it with my Fire Emblem game project so that I could have some form of dialogue.

* Visual Novel Playlist Link: <https://www.youtube.com/playlist?list=PLGSox0FgA5B7mApF1vhbspLj5NpzKedU6>
* Fire Emblem Project: <https://sharemygame.com/@wsan/frozen-embers>

This system boasted a basic scripting language that allowed a person to feed the game a formatted textfile that the System could read and parse (see sample.txt). It also allowed for the inclusion of functions to add even greater flexiblity.

> This is a small part of the very large codebase that was the Fire Emblem Project.

## Problem

The original VN system was very robust and very flexible. However, I did not like how it parsed functions through HandleAction (NovelController(Original):445). Originally, it used a large switch statement that cycled through string names which corresponded to functions defined in the NovelController.

```csharp
public void HandleAction(string action){
    string[] data = action.Split('(',')');
    switch(data[0]) {
        case "enter":
            Command_Enter(data[1]);
            break;
        case "exit":
            Command_Exit(data[1]);
            break;
        ...
    }
}
```

There were 2 main reasons why I did not like it:

* It scales poorly.
  * I have to add a new switch case and ensure it maps to the correct function every time.
    * If I add a new function and forget to add the case, I'll get a runtime error at best.
  * I have to look at TWO places when I add new functionality, instead of the ONE place that I should care about, the new function.
    * I shouldn't have to care about a new switch case.
* It fails to take full advantage of C#'s Object Oriented Design.
  * We're not savages! We have dynamic dispatch!

___

## Solution

Taking full advantage of C#, I created a CommandLibrary file that stores all new commands for the VN system. Every new command inherits from the abstract VN_Command class and implements 2 things:

* A name for the function.
* Its own version of Call_Command which takes a string of arguments.

```csharp
public class Command_StopMusic : VN_Command {
    public Command_StopMusic() {
        name = "stopMusic";
    }
    public override void Call_Command(string args) {
        AudioManager.instance.PlaySong(null);
    }
}
```

Using Reflection, I find all non-abstract child classes of VN_Command. Then for each unique class I find, I instantiate it and add it to a dictionary where its name is the key and the value is the instance itself.

```csharp
var commands = from t in Assembly.GetExecutingAssembly().GetTypes() where t.IsClass && !t.IsAbstract && t.Namespace == libNamespace select t;
commandLibrary = new Dictionary<string, VN_Command>();
foreach (var command in commands){
    var x = Activator.CreateInstance(command) as VN_Command;
    commandLibrary.Add(x.name, x);
}
```

### "But isn't Reflection expensive to use?"

> Certainly, Reflection can be an expensive thing to use, but like all things, it's a great tool so long as you know when to use it. And given that all instances of NovelController share a single static instance of VN_CommandLibrary, Reflection will only ever be used once at the first instance of NovelController (NovelController:28). VN_CommandLibrary is not instantiated anywhere else in the code.

This structure means that every new function I want to add gets its own class, and this class will automatically be added to the dictionary at runtime. And so, in the NovelController, the giant switch statement gets reduced to this:

```csharp
public void HandleAction(string action) {
    string[] localData = action.Split('(', ')');
    if (VN_CommandLibrary.commandLibrary.ContainsKey(localData[0]))
        VN_CommandLibrary.commandLibrary[localData[0]].Call_Command(localData[1]);
    else
        Debug.LogError("Error. Command " + localData[0] + " does not exist!");
}
```

There are 2 main reasons why I think this structure is better:

* It scales very well.
  * If I need a new function for the scripting language, I just have to create a new class.
  * Related classes can share a parent with all the base functionality stored there and the child class can concern itself with things unique to it.
* It taks advantage of dictionaries & dynamic dispatch
  * Since dictionaries have O(1) to Search, I can quickly call the relevant instance of VN_Command that I need.
  * Dynamic Dispatch will allow the code to decide at runtime which version of Call_Command it should use based on the instance fed from the dictionary.
