using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using System.Linq;
using Command_Library;

/// <summary>
/// Central class that accesses the Command Library.
/// </summary>
public class VN_CommandLibrary
{
    public Dictionary<string, VN_Command> commandLibrary;
    public string libNamespace = "Command_Library";
    public VN_CommandLibrary()
    {
        // Get all classes of type VN_Command (including the child classes) that are NOT abstract
        var commands = from t in Assembly.GetExecutingAssembly().GetTypes() where t.IsClass && !t.IsAbstract && t.Namespace == libNamespace select t;
        commandLibrary = new Dictionary<string, VN_Command>();
        // Create an instance of each class and add it to the dictionary.
        foreach (var command in commands)
        {
            var x = Activator.CreateInstance(command) as VN_Command;
            commandLibrary.Add(x.name, x);
        }
    }
}

namespace Command_Library
{
    #region Abstract Command Class
    public abstract class VN_Command
    {
        public string name;
        protected string command;
        public abstract void Call_Command(string args);
    }
    #endregion

    #region Set Layer Image
    public abstract class Command_SetLayerImage : VN_Command
    {
        protected LayerController.LAYER layer;
        public override void Call_Command(string args)
        {
            string textureName = args.Contains(",") ? args.Split(',')[0] : args;
            Texture2D tex = getTexture(textureName);
            float spd = args.Contains(",") ? float.Parse(args.Split(',')[1]) : 5f;
            layer.TransitionToTexture(tex, spd);
        }

        protected Texture2D getTexture(string textureName)
        {
            string path = "Images/UI/Backdrops/still/";
            Texture2D tex = null;
            if (textureName != "null")
            {
                tex = Resources.Load(path + textureName) as Texture2D;
                if (tex == null)
                {
                    path = "Images/UI/Backdrops/animated/";
                    tex = Resources.Load(path + textureName) as Texture2D;
                }
                if (tex == null)
                {
                    Debug.LogWarning("Unable to find texture, " + textureName + ", in the Backdrops folder.");
                }
            }
            return tex;
        }
    }

    public class Command_SetBackground : Command_SetLayerImage
    {
        public Command_SetBackground()
        {
            name = "setBackground";
            layer = LayerController.instance.background;
        }
    }

    public class Command_SetCinematic : Command_SetLayerImage
    {
        public Command_SetCinematic()
        {
            layer = LayerController.instance.cinematics;
            name = "setCinematic";
        }
    }

    public class Command_SetForeground : Command_SetLayerImage
    {
        public Command_SetForeground()
        {
            name = "setForeground";
            layer = LayerController.instance.foreground;
        }
    }
    #endregion

    #region Transition Layers (Background, Foreground, Cinematic)
    public abstract class Command_TransLayerImage : VN_Command
    {
        protected LayerController.LAYER layer;
        public override void Call_Command(string args)
        {
            string[] parameters = args.Split(',');
            string texName = parameters[0];
            string transTexName = parameters[1];

            Texture2D tex = texName == "null" ? null : getTexture(texName);
            Texture2D transTex = Resources.Load("Images/TransitionEffects/" + transTexName) as Texture2D;

            float spd = 2f;
            if (parameters.Length >= 3)
            {
                string p = parameters[2]; // optional float parameter
                float fVal = 0f;
                if (float.TryParse(p, out fVal))
                { spd = fVal; }
            }
            TransitionController.TransitionLayer(layer, tex, transTex, spd);
        }
    }

    public class Command_TransBackground : Command_TransLayerImage
    {
        public Command_TransBackground()
        {
            name = "transBackground";
            layer = LayerController.instance.background;
        }
    }

    public class Command_TransCinematic : Command_TransLayerImage
    {
        public Command_TransCinematic()
        {
            name = "transCinematic";
            layer = LayerController.instance.cinematics;
        }
    }

    public class Command_TransForeground : Command_TransLayerImage
    {
        public Command_TransForeground()
        {
            name = "transForeground";
            layer = LayerController.instance.foreground;
        }
    }
    #endregion

    #region Flip Characters
    public abstract class Command_FlipCharacter : VN_Command
    {
        public override void Call_Command(string args)
        {
            string[] characters = args.Split(',');
            Character character;
            foreach (string c in characters)
            {
                character = CharacterManager.instance.GetCharacter(c);
                Flip(character);
            }
        }
        protected abstract void Flip(Character c);
    }

    public class Command_Flip : Command_FlipCharacter
    {
        public Command_Flip()
        {
            name = "flip";
        }
        protected override void Flip(Character c)
        {
            c.Flip();
        }
    }

    public class Command_LeftFlip : Command_FlipCharacter
    {
        public Command_LeftFlip()
        {
            name = "faceLeft";
        }
        protected override void Flip(Character c)
        {
            c.FaceLeft();
        }
    }


    public class Command_RightFlip : Command_FlipCharacter
    {
        public Command_RightFlip()
        {
            name = "faceRight";
        }
        protected override void Flip(Character c)
        {
            c.FaceRight();
        }
    }
    #endregion

    #region Play Sounds (SFX, Music)
    public abstract class Command_PlaySound : VN_Command
    {
        protected string folderPath;
        public override void Call_Command(string args)
        {
            string sound = args.Split(',')[0];
            string path = folderPath + sound;
            AudioClip clip = Resources.Load(path) as AudioClip;
            if (clip != null)
            {
                PlaySound(clip);
            }
            else
            {
                ErrorMSG(sound, path);
            }
        }
        public abstract void PlaySound(AudioClip clip);
        public abstract void ErrorMSG(string sound, string path);
    }

    public class Command_PlaySFX : Command_PlaySound
    {
        public Command_PlaySFX()
        {
            name = "playSFX";
            folderPath = "Audio/SFX/";
        }
        public override void PlaySound(AudioClip clip)
        {
            AudioManagerBase.instance.PlaySFX(clip);
        }
        public override void ErrorMSG(string sound, string path)
        {
            Debug.LogError("The SFX clip called " + sound + " could not be found at: " + path);
        }
    }

    public class Command_PlayMusic : Command_PlaySound
    {
        public Command_PlayMusic()
        {
            name = "playMusic";
            folderPath = "Audio/Music/";
        }
        public override void PlaySound(AudioClip clip)
        {
            AudioManagerBase.instance.PlaySong(clip);
        }
        public override void ErrorMSG(string sound, string path)
        {
            Debug.LogError("The music clip called " + sound + " could not be found at: " + path);
        }
    }

    public class Command_StopMusic : VN_Command
    {
        public Command_StopMusic()
        {
            name = "stopMusic";
        }

        public override void Call_Command(string args)
        {
            AudioManager.instance.PlaySong(null);
        }
    }
    #endregion

    #region Move Characters ( MoveCharacter, MoveCharacterTo, SetCharacterPositionTo )
    /// <summary>
    /// Abstract class for moving a Character in a scene.
    /// </summary>
    public abstract class Command_Move : VN_Command
    {
        public override void Call_Command(string data)
        {
            string[] args = data.Split(',');
            Move(args);
        }
        public abstract void Move(string[] args);
    }

    /// <summary>
    /// Move Character to a given position from a set position.
    /// </summary>
    public class Command_MoveCharacter : Command_Move
    {
        public Command_MoveCharacter()
        {
            name = "move";
        }
        public override void Move(string[] args)
        {
            string character = args[0];
            Vector2 direction = CharacterManager.stagePositions[args[1]];
            float speed = args.Length == 4 ? float.Parse(args[2]) : 2f;

            Character c = CharacterManager.instance.GetCharacter(character);
            c.MoveTo(direction, speed);
        }
    }

    /// <summary>
    /// Move Character to a given position from current position.
    /// </summary>
    public class Command_MoveCharacterTo : Command_Move
    {
        public Command_MoveCharacterTo()
        {
            name = "moveTo";
        }
        public override void Move(string[] args)
        {
            string character = args[0];
            Vector2 start = CharacterManager.stagePositions[args[1]];
            Vector2 end = CharacterManager.stagePositions[args[2]];
            float speed = args.Length == 4 ? float.Parse(args[3]) : 2f;

            Character c = CharacterManager.instance.GetCharacter(character);
            c.MoveTo(start, end, speed);
        }
    }

    public class Command_SetCharacterPositionTo : Command_Move
    {
        public Command_SetCharacterPositionTo()
        {
            name = "setPosition";
        }
        public override void Move(string[] args)
        {
            string character = args[0];
            Vector2 position = CharacterManager.stagePositions[args[1]];
            Character c = CharacterManager.instance.GetCharacter(character);
            c.SetPosition(position);
        }
    }
    #endregion

    #region Set Expressions
    public class Command_SetExpression : VN_Command
    {
        public Command_SetExpression()
        {
            name = "setExpression";
        }
        public override void Call_Command(string args)
        {
            string[] parameters = args.Split(',');
            string character = parameters[0];
            string region = parameters[1];
            string expression = parameters[2];
            float speed = parameters.Length == 4 ? float.Parse(parameters[3]) : 5f;

            Character c = CharacterManager.instance.GetCharacter(character);
            Sprite sprite = c.GetSprite(expression); // Finds the expression from Sprites
            switch (region.ToLower())
            {
                case "body":
                    c.TransitionBody(sprite, speed);
                    break;
                case "face":
                    c.TransitionFace(sprite, speed);
                    break;
                default: // TODO: Eventually, this needs to ONLY accept face.
                    Debug.LogError("Invalid expression set at region: " + region + ". This function only accepts 'face' or 'body'.");
                    break;
            }
        }
    }
    #endregion

    #region Enter / Exit Character
    public abstract class Command_CharacterEntry : VN_Command
    {
        protected string[] parameters;
        protected string[] characters;
        protected float speed = 3f;
        public override void Call_Command(string args)
        {
            parameters = args.Split(',');
            characters = parameters[0].Split(';');
            speed = 3f;
            Transition();
        }
        public abstract void Transition();
    }

    public class Command_Enter : Command_CharacterEntry
    {
        public Command_Enter()
        {
            name = "enter";
        }
        public override void Transition()
        {
            for (int i = 1; i < parameters.Length; i++)
            {
                float fVal = 0f;
                if (float.TryParse(parameters[i], out fVal))
                { speed = fVal; }
            }
            foreach (string s in characters)
            {
                Character c = CharacterManager.instance.GetCharacter(s, false);
                if (!c.enabled)
                {
                    c.renderers.bodyRenderer.color = new Color(1, 1, 1, 0);
                    c.renderers.faceRenderer.color = new Color(1, 1, 1, 0);
                    c.enabled = true;

                    c.TransitionBody(c.renderers.bodyRenderer.sprite, speed);
                    c.TransitionFace(c.renderers.faceRenderer.sprite, speed);
                }
                else
                {
                    c.FadeIn(speed);
                }
            }
        }
    }

    public class Command_Exit : Command_CharacterEntry
    {
        public Command_Exit()
        {
            name = "exit";
        }
        public override void Transition()
        {
            for (int i = 1; i < parameters.Length; i++)
            {
                float fVal = 0f;
                if (float.TryParse(parameters[i], out fVal))
                {
                    speed = fVal;
                }
            }
            foreach (string s in characters)
            {
                Character c = CharacterManager.instance.GetCharacter(s);
                c.FadeOut(speed);
            }
        }
    }
    #endregion

    #region Show Scene
    public class Command_ShowScene : VN_Command
    {
        public Command_ShowScene()
        {
            name = "showScene";
        }
        public override void Call_Command(string args)
        {
            string[] parameters = args.Split(',');
            bool show = bool.Parse(parameters[0]);
            string texName = parameters[1];
            Texture2D transTex = Resources.Load("Images/TransitionEffects/" + texName) as Texture2D;
            float spd = 2f;
            if (parameters.Length >= 3)
            {
                string p = parameters[2]; // optional float parameter
                float fVal = 0f;
                if (float.TryParse(p, out fVal))
                { spd = fVal; }
            }
            TransitionController.ShowScene(show, spd, transTex);
        }
    }
    #endregion

    #region Load Next Scene
    public class Command_LoadScene : VN_Command
    {
        public Command_LoadScene()
        {
            name = "load";
        }
        public override void Call_Command(string args)
        {
            NovelController.instance.LoadChapterFile(args);
        }
    }
    #endregion

    #region Start Game
    public class Command_StartGame : VN_Command
    {
        public Command_StartGame()
        {
            name = "startGame";
        }
        public override void Call_Command(string args)
        {
            if (GridSystem.instance != null)
            {
                GridSystem.instance.InitGame();
            }
            else
            {
                Debug.LogWarning("GridSystem is missing!");
            }
        }
    }
    #endregion

    #region Win/Loss
    public class Command_Win : VN_Command
    {
        public Command_Win()
        {
            name = "winGame";
        }

        public override void Call_Command(string args)
        {
            WLController.instance.displayWin();
        }
    }

    public class Command_Loss : VN_Command
    {
        public Command_Loss()
        {
            name = "loseGame";
        }

        public override void Call_Command(string args)
        {
            WLController.instance.displayLoss();
        }
    }
    #endregion

}