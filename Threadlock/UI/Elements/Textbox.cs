using Nez;
using Nez.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Models;
using Threadlock.SaveData;

namespace Threadlock.UI.Elements
{
    public class Textbox : Table
    {
        Skin _skin;
        int _fontSize = 18;

        //elements
        Label _textLabel;

        ICoroutine _readTextCoroutine;

        public Textbox(Skin skin)
        {
            _skin = skin;

            SetBackground(_skin.GetDrawable("window_blue"));

            SetWidth(Game1.ResolutionManager.UIResolution.X * .7f);

            PadTop(Value.PercentWidth(.04f));
            PadBottom(Value.PercentWidth(.04f));
            PadLeft(Value.PercentWidth(.07f));
            PadRight(Value.PercentWidth(.07f));

            SetHeight(GetPadTop() + GetPadBottom() + (_fontSize * 3));

            //SetSize(Game1.ResolutionManager.UIResolution.X * .7f, GetPadTop() + GetPadBottom() + (_fontSize * 3));

            _textLabel = new Label("", _skin, $"abaddon_{_fontSize}").SetAlignment(Nez.UI.Align.TopLeft);
            Add(_textLabel).Expand().Top().Left();
        }

        public IEnumerator DisplayText(List<DialogueLine> lines)
        {
            var maxWidth = GetWidth() - GetPadRight() - GetPadLeft();

            var prevLineCount = 0;
            foreach (var line in lines)
            {
                //create a copy label with the text
                var labelCopy = new Label("", _textLabel.GetStyle());
                labelCopy.SetVisible(false);

                var sb = new StringBuilder();
                var words = line.Text.Split(' ');
                var lineCount = 1;
                foreach (var word in words)
                {
                    labelCopy.SetText(sb.ToString() + word + " ");
                    labelCopy.Pack();

                    if (labelCopy.GetWidth() < maxWidth)
                        sb.Append(word + " ");
                    else
                    {
                        sb.Append("\n" + word + " ");
                        lineCount++;
                    }
                }

                if (prevLineCount != 0 && lineCount != prevLineCount)
                    SetHeight(GetPadTop() + GetPadBottom() + (_fontSize * Math.Max(3, lineCount)));

                var wrappedText = sb.ToString();

                _readTextCoroutine = Game1.StartCoroutine(ReadText(wrappedText, _textLabel));
                yield return _readTextCoroutine;

                //wait one frame here so we don't immediately continue if text was skipped
                yield return null;

                while (!Controls.Instance.Check.IsPressed)
                    yield return null;

                prevLineCount = lineCount;
            }
        }

        IEnumerator ReadText(string text, Label label)
        {
            int count = 1;
            float characterInterval = .04f;
            float soundInterval = .05f;
            float timer = 0;
            float soundTimer = 0;

            while (count <= text.Length)
            {
                //check if we should skip text
                if (timer > 0 && Controls.Instance.Check.IsPressed)
                {
                    label.SetText(text);
                    break;
                }

                //increment timers
                timer += Time.DeltaTime;
                soundTimer += Time.DeltaTime;

                //when timer reachers character interval, add character
                if (timer >= characterInterval)
                {
                    label.SetText(text.Substring(0, count));

                    //play sound if enough time has passed since last sound
                    if (soundTimer >= soundInterval)
                    {
                        Game1.AudioManager.PlaySound(Content.Audio.Sounds.Default_text);
                        soundTimer = 0;
                    }

                    //set timer to 0 and increment counter
                    timer = 0;
                    count++;

                    //adjust characterInterval if the last character was a comma for extra pause
                    characterInterval = label.GetText().Last() == ',' ? .2f : .04f;
                }

                yield return null;
            }
        }
    }
}
