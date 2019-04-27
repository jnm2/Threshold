using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Threshold
{
    public static class ConsoleUtils
    {
        private static readonly IReadOnlyDictionary<char, string> YesNoChoices = new Dictionary<char, string>
        {
            ['y'] = "yes",
            ['n'] = "no"
        };

        public static bool ChooseYesNo(string message)
        {
            Console.Write(message.TrimEnd() + ' ');

            return ReadChoice(YesNoChoices) == 'y';
        }

        public static char Choose(string message)
        {
            var options = Regex.Matches(message, @"\[(?<char>[^\[\]])\](?<rest>[\w+ ]*\w)?");
            if (options.Count < 2)
                throw new ArgumentException("At least two options must be specified.", nameof(message));

            var fullTextByOptionChar = new Dictionary<char, string>();

            foreach (Match option in options)
            {
                var c = message[option.Groups["char"].Index];
                var fullText = c + option.Groups["rest"].Value;
                if (fullText.EndsWith(" or", StringComparison.OrdinalIgnoreCase))
                    fullText = fullText.Substring(0, fullText.Length - 3);

                if (!fullTextByOptionChar.TryAdd(c, fullText))
                {
                    throw new ArgumentException("Each option must be a unique character.", nameof(message));
                }
            }

            DisplayChoice(message, options);

            return ReadChoice(fullTextByOptionChar);
        }

        private static char ReadChoice(IReadOnlyDictionary<char, string> fullTextByOptionChar)
        {
            var chosen = '\0';
            var currentText = string.Empty;

            while (true)
            {
                var key = Console.ReadKey(intercept: true);

                switch (key.Key)
                {
                    case ConsoleKey.Backspace:
                    case ConsoleKey.Escape:
                    case ConsoleKey.LeftArrow:
                    case ConsoleKey.Home:
                        if (chosen != '\0')
                        {
                            ReplaceText(currentText.Length, null);
                            chosen = '\0';
                            currentText = string.Empty;
                        }
                        else
                        {
                            Console.Beep();
                        }
                        break;

                    case ConsoleKey.Enter:
                        if (chosen != '\0')
                        {
                            Console.WriteLine();
                            return chosen;
                        }
                        Console.Beep();
                        break;

                    default:
                        if (fullTextByOptionChar.TryGetValue(key.KeyChar, out var fullText))
                        {
                            ReplaceText(currentText.Length, fullText);
                            chosen = key.KeyChar;
                            currentText = fullText;
                        }
                        else
                        {
                            ReplaceText(currentText.Length, null);
                            Console.Beep();
                            chosen = '\0';
                            currentText = string.Empty;
                        }
                        break;
                }
            }
        }

        private static void ReplaceText(int currentLength, string newText)
        {
            newText ??= string.Empty;

            var lengthToErase = Math.Max(0, currentLength - newText.Length);
            var atomicBuffer = new char[lengthToErase * 2 + currentLength + newText.Length];

            for (var i = 0; i < lengthToErase; i++)
            {
                atomicBuffer[i] = '\b';
                atomicBuffer[lengthToErase + i] = ' ';
            }

            for (var i = 0; i < currentLength; i++)
            {
                atomicBuffer[lengthToErase * 2 + i] = '\b';
            }

            newText.CopyTo(0, atomicBuffer, lengthToErase * 2 + currentLength, newText.Length);

            Console.Write(atomicBuffer);
        }

        private static void DisplayChoice(string message, MatchCollection options)
        {
            var nextIndex = 0;
            foreach (Match option in options)
            {
                Console.Write(message.Substring(nextIndex, option.Index - nextIndex));

                var backgroundIsBlack = Console.BackgroundColor == ConsoleColor.Black;

                using (backgroundIsBlack ? WithBackgroundColor(ConsoleColor.Blue) : null)
                {
                    using (WithForegroundColor(ConsoleColor.DarkGray))
                    {
                        Console.Write('[');
                    }

                    using (backgroundIsBlack ? WithForegroundColor(ConsoleColor.White) : null)
                    {
                        Console.Write(message[option.Groups["char"].Index]);
                    }

                    using (WithForegroundColor(ConsoleColor.DarkGray))
                    {
                        Console.Write(']');
                    }
                }

                nextIndex = option.Groups["rest"].Index;
            }

            Console.Write(message.Substring(nextIndex).TrimEnd() + ' ');
        }

        public static IDisposable WithForegroundColor(ConsoleColor foregroundColor)
        {
            var initial = Console.ForegroundColor;
            Console.ForegroundColor = foregroundColor;
            return On.Dispose(() => Console.ForegroundColor = initial);
        }

        public static IDisposable WithBackgroundColor(ConsoleColor backgroundColor)
        {
            var initial = Console.BackgroundColor;
            Console.BackgroundColor = backgroundColor;
            return On.Dispose(() => Console.BackgroundColor = initial);
        }

        public static void EditMultilineMessage(StringBuilder message)
        {
            const string instructionMessage = "Type as many lines as you like below. When the text is complete, press Esc.";
            var messageSeparator = new string('─', instructionMessage.Length);
            var blankLine = new string(' ', instructionMessage.Length);

            var currentLine = new StringBuilder();
            var lines = new List<StringBuilder> { currentLine };

            foreach (var chunk in message.GetChunks())
            {
                var remaining = chunk.Span;

                while (true)
                {
                    var end = remaining.IndexOf('\n');
                    if (end == -1)
                    {
                        currentLine.Append(remaining);
                        break;
                    }

                    currentLine.Append(remaining.Slice(0, end));
                    currentLine = new StringBuilder();
                    lines.Add(currentLine);
                    remaining = remaining.Slice(end + 1);
                }
            }

            Console.WriteLine(instructionMessage);
            Console.WriteLine(messageSeparator);

            var editStartLine = Console.CursorTop;
            var endingSeparatorLine = -1;

            var lineIndex = 0;
            var charIndex = 0;

            var escape = false;

            while (true)
            {
                Console.CursorVisible = false;
                try
                {
                    Console.SetCursorPosition(0, editStartLine);

                    foreach (var line in lines)
                    {
                        Console.Write(line);
                        Console.Write(new string(' ', line.Length == 0
                            ? Console.BufferWidth
                            : Console.BufferWidth - Console.CursorLeft));
                    }

                    Console.Write(messageSeparator);

                    if (escape)
                    {
                        Console.WriteLine();
                        break;
                    }

                    var lastEndingSeparatorLine = endingSeparatorLine;
                    endingSeparatorLine = Console.CursorTop;

                    if (endingSeparatorLine < lastEndingSeparatorLine)
                    {
                        Console.WriteLine();
                        Console.Write(blankLine);
                    }

                    while (true)
                    {
                        var bufferWidth = Console.BufferWidth;

                        var wrappedLineCount = 0;

                        for (var i = 0; i < lineIndex; i++)
                        {
                            wrappedLineCount++;
                            wrappedLineCount += lines[i].Length / bufferWidth;
                        }

                        wrappedLineCount += charIndex / bufferWidth;

                        if (bufferWidth == Console.BufferWidth)
                        {
                            Console.SetCursorPosition(charIndex % bufferWidth, editStartLine + wrappedLineCount);
                            break;
                        }
                    }
                }
                finally
                {
                    Console.CursorVisible = true;
                }

                var keyPress = Console.ReadKey(intercept: true);
                switch (keyPress.Key)
                {
                    case ConsoleKey.Backspace:
                        if (charIndex > 0)
                        {
                            charIndex--;
                            lines[lineIndex].Remove(charIndex, 1);
                        }
                        else if (lineIndex > 0)
                        {
                            var previousLine = lines[lineIndex - 1];
                            charIndex = previousLine.Length;
                            previousLine.Append(lines[lineIndex]);
                            lines.RemoveAt(lineIndex);
                            lineIndex--;
                        }
                        else
                        {
                            Console.Beep();
                        }

                        break;

                    case ConsoleKey.Delete:
                        if (charIndex < lines[lineIndex].Length)
                        {
                            lines[lineIndex].Remove(charIndex, 1);
                        }
                        else if (lineIndex < lines.Count - 1)
                        {
                            lines[lineIndex].Append(lines[lineIndex + 1]);
                            lines.RemoveAt(lineIndex + 1);
                        }
                        else
                        {
                            Console.Beep();
                        }

                        break;

                    case ConsoleKey.Enter:
                        var line = lines[lineIndex];
                        var newLine = new StringBuilder();
                        newLine.Append(line, charIndex, line.Length - charIndex);
                        line.Remove(charIndex, line.Length - charIndex);

                        lineIndex++;
                        charIndex = 0;
                        lines.Insert(lineIndex, newLine);
                        break;

                    case ConsoleKey.Escape:
                        escape = true;
                        break;

                    case ConsoleKey.Tab:
                        var numSpaces = ((lines[lineIndex].Length + 3) % 4) + 1;

                        lines[lineIndex].Insert(charIndex, " ", numSpaces);
                        charIndex += numSpaces;
                        break;

                    case ConsoleKey.Home:
                        if (keyPress.Modifiers.HasFlag(ConsoleModifiers.Control))
                            lineIndex = 0;
                        charIndex = 0;
                        break;

                    case ConsoleKey.End:
                        if (keyPress.Modifiers.HasFlag(ConsoleModifiers.Control))
                            lineIndex = lines.Count - 1;
                        charIndex = lines[lineIndex].Length;
                        break;

                    case ConsoleKey.LeftArrow:
                        if (charIndex > 0)
                        {
                            charIndex--;
                        }
                        else if (lineIndex > 0)
                        {
                            lineIndex--;
                            charIndex = lines[lineIndex].Length;
                        }
                        else
                        {
                            Console.Beep();
                        }
                        break;

                    case ConsoleKey.RightArrow:
                        if (charIndex < lines[lineIndex].Length)
                        {
                            charIndex++;
                        }
                        else if (lineIndex < lines.Count - 1)
                        {
                            lineIndex++;
                            charIndex = 0;
                        }
                        else
                        {
                            Console.Beep();
                        }
                        break;

                    case ConsoleKey.UpArrow:
                        if (lineIndex > 0)
                        {
                            lineIndex--;
                            charIndex = Math.Min(charIndex, lines[lineIndex].Length);
                        }
                        else
                        {
                            Console.Beep();
                        }
                        break;

                    case ConsoleKey.DownArrow:
                        if (lineIndex < lines.Count - 1)
                        {
                            lineIndex++;
                            charIndex = Math.Min(charIndex, lines[lineIndex].Length);
                        }
                        else
                        {
                            Console.Beep();
                        }
                        break;

                    case var _ when keyPress.KeyChar != '\0':
                        lines[lineIndex].Insert(charIndex, keyPress.KeyChar);
                        charIndex++;
                        break;
                }
            }

            message.Clear();

            for (var i = 0; i < lines.Count; i++)
            {
                if (i != 0) message.Append('\n');
                message.Append(lines[i]);
            }
        }

        public static string GetRequiredDescription()
        {
            ConsoleUtils.WriteLineWithWordBreaks("A description line is required. Make sure it contains instructions that are fully understandable when you are not available or it is being read in an emergency. ");

            while (true)
            {
                Console.WriteLine("At least 10 non-space characters are required.");
                Console.Write("Description: ");

                var line = Console.ReadLine().Trim();

                if (line.Where(c => !char.IsWhiteSpace(c)).Take(10).Count() == 10)
                {
                    return line;
                }
            }
        }

        public static string GetNonWhitespaceLine(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                Console.Write(' ');

                var line = Console.ReadLine().Trim();

                if (line.Length != 0)
                {
                    return line;
                }
            }
        }

        public static int GetPositiveInteger(string prompt, int max)
        {
            while (true)
            {
                Console.Write(prompt);
                Console.Write(' ');

                if (int.TryParse(Console.ReadLine(), out var number) && number >= 1 && number <= max)
                {
                    return number;
                }

                Console.WriteLine($"Number must be between 1 and {max}.");
            }
        }

        public static void WriteLineWithWordBreaks(string value)
        {
            for (var next = 0;;)
            {
                var wordEnd = value.IndexOfAny(new[] { ' ', '\r', '\n' }, next);

                var lengthToWrite = wordEnd == -1 ? value.Length - next : wordEnd + 1 - next;

                if (Console.BufferWidth - Console.CursorLeft < lengthToWrite)
                    Console.WriteLine();

                Console.Write(value.Substring(next, lengthToWrite));

                if (wordEnd == -1) break;
                next = wordEnd + 1;
            }

            Console.WriteLine();
        }
    }
}
