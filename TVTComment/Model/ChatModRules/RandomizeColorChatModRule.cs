using System;
using System.Collections.Generic;
using System.Drawing;

namespace TVTComment.Model.ChatModRules
{
    class RandomizeColorChatModRule : IChatModRule
    {
        private readonly Random random = new Random();

        public string Description => "色をランダムに変更";
        public IEnumerable<ChatCollectServiceEntry.IChatCollectServiceEntry> TargetChatCollectServiceEntries { get; }

        public RandomizeColorChatModRule(IEnumerable<ChatCollectServiceEntry.IChatCollectServiceEntry> targetChatCollectServiceEntries)
        {
            TargetChatCollectServiceEntries = targetChatCollectServiceEntries;
        }

        public bool Modify(Chat chat)
        {
            chat.SetColor(ColorFromHls(random.Next(0, 360), 0.7f, 1));
            return true;
        }

        private static Color ColorFromHls(float hue, float lightness, float saturation)
        {
            hue %= 360;
            if (lightness < 0 || 1 < lightness)
            {
                throw new ArgumentOutOfRangeException(nameof(lightness), $"{nameof(lightness)} must be from 0.0 to 1.0");
            }
            if (saturation < 0 || 1 < saturation)
            {
                throw new ArgumentOutOfRangeException(nameof(saturation), $"{nameof(saturation)} must be from 0.0 to 1.0");
            }

            float s = saturation;
            float l = lightness;

            float r1, g1, b1;
            if (s == 0)
            {
                r1 = l;
                g1 = l;
                b1 = l;
            }
            else
            {
                float h = hue / 60f;
                int i = (int)Math.Floor(h);
                float f = h - i;
                //float c = (1f - Math.Abs(2f * l - 1f)) * s;
                float c;
                if (l < 0.5f)
                {
                    c = 2f * s * l;
                }
                else
                {
                    c = 2f * s * (1f - l);
                }
                float m = l - c / 2f;
                float p = c + m;
                //float x = c * (1f - Math.Abs(h % 2f - 1f));
                float q; // q = x + m
                if (i % 2 == 0)
                {
                    q = l + c * (f - 0.5f);
                }
                else
                {
                    q = l - c * (f - 0.5f);
                }

                switch (i)
                {
                    case 0:
                        r1 = p;
                        g1 = q;
                        b1 = m;
                        break;
                    case 1:
                        r1 = q;
                        g1 = p;
                        b1 = m;
                        break;
                    case 2:
                        r1 = m;
                        g1 = p;
                        b1 = q;
                        break;
                    case 3:
                        r1 = m;
                        g1 = q;
                        b1 = p;
                        break;
                    case 4:
                        r1 = q;
                        g1 = m;
                        b1 = p;
                        break;
                    case 5:
                        r1 = p;
                        g1 = m;
                        b1 = q;
                        break;
                    default:
                        throw new ArgumentException($"Value of {nameof(hue)} is invalid", nameof(hue));
                }
            }

            return Color.FromArgb(
                (int)Math.Round(r1 * 255f),
                (int)Math.Round(g1 * 255f),
                (int)Math.Round(b1 * 255f));
        }
    }
}
