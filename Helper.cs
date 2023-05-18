using AWK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WebSocketSharp;

namespace AwkwardMP
{
    internal class Helper
    {
        public static Dictionary<string, Sprite> CachedSprites = new();

        public static Sprite ConnectedSprite;
        public static Sprite DisconnectedSprite;

        public static Sprite GetConnectedSprite()
        {
            if (ConnectedSprite) return ConnectedSprite;
            return ConnectedSprite = loadSpriteFromResources("AwkwardMP.Resources.Tick.png", 150f);
        }

        public static Sprite GetDisconnectedSprite()
        {
            if (DisconnectedSprite) return DisconnectedSprite;
            return DisconnectedSprite = loadSpriteFromResources("AwkwardMP.Resources.Cross.png", 150f);
        }

        public static Sprite loadSpriteFromResources(string path, float pixelsPerUnit)
        {
            try
            {
                if (CachedSprites.TryGetValue(path + pixelsPerUnit, out var sprite)) return sprite;
                Texture2D texture = loadTextureFromResources(path);
                sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
                sprite.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;
                return CachedSprites[path + pixelsPerUnit] = sprite;
            }
            catch
            {
                AwkwardMP.Log.LogError("Error loading sprite from path: " + path);
            }
            return null;
        }

        public static unsafe Texture2D loadTextureFromResources(string path)
        {
            try
            {
                Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, true);
                Assembly assembly = Assembly.GetExecutingAssembly();
                Stream stream = assembly.GetManifestResourceStream(path);
                var length = stream.Length;
                byte[] byteTexture = new byte[stream.Length];
                stream.Read(byteTexture, 0, byteTexture.Length);

                texture.LoadImage(byteTexture, false);
                return texture;
            }
            catch
            {
                AwkwardMP.Log.LogError("Error loading texture from resources: " + path);
            }
            return null;
        }

        public static object CurrentQuestion()
        {
            object _params = new
            {
                title = "",
                answerA = "",
                answerB = "",
            };

            try
            {
                QuestionState question = Globals.GameState.GetCurrentQuestion();
                _params = new
                {
                    title = question.QuestionText,
                    answerA = question.Answer1Text,
                    answerB = question.Answer2Text,
                };

                return _params;

            } catch (Exception ex){
                AwkwardMP.Log.LogError(ex);
                return _params; 
            }
        }
    }
}
