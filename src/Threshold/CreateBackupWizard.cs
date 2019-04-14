using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;

namespace Threshold
{
    internal sealed class CreateBackupWizard
    {
        private readonly List<BackupItem> items = new List<BackupItem>
        {
            new BackupItem(
                "Instructions - read first",
                BackupContentType.Text,
                fileName: null,
                ThresholdBackup.Utf8NoBom.GetBytes("[Warning! You might not be the same person who restores the items in this backup. Replace this text with instructions that are fully understandable in case of emergency.]"))
        };

        public void Run()
        {
            EditItem();
            Console.WriteLine();

            while (true)
            {
                switch (ChooseBackupEditOption())
                {
                    case 'a':
                        AddItem(out var quit);
                        if (quit) return;
                        Console.WriteLine();
                        break;

                    case 'e':
                        EditItem();
                        Console.WriteLine();
                        break;

                    case 'd':
                        DiscardItem();
                        Console.WriteLine();
                        break;

                    case 'l':
                        ListItems();
                        Console.WriteLine();
                        break;

                    case 'p':
                        PrintBackup();
                        Console.WriteLine();
                        break;

                    case 'q':
                        if (!items.Any() || ConsoleUtils.ChooseYesNo("Are you sure you want to discard the current backup without printing?"))
                        {
                            return;
                        }
                        break;
                }

                Console.WriteLine();
            }
        }

        private char ChooseBackupEditOption()
        {
            if (items.Count == 0) return 'a';

            Console.WriteLine(
                "There "
                + (items.Count == 1 ? "is one item" : $"are {items.Count} items")
                + " ready to back up.");

            return ConsoleUtils.Choose(
                "Would you like to:" + Environment.NewLine
                 + "  [a]dd an item," + Environment.NewLine
                 + "  [e]dit an item," + Environment.NewLine
                 + "  [d]iscard an item," + Environment.NewLine
                 + "  [l]ist all items," + Environment.NewLine
                 + "  [p]rint the backup," + Environment.NewLine
                 + "  or [q]uit?");
        }

        private void AddItem(out bool quit)
        {
            quit = false;

            switch (ConsoleUtils.Choose(
                "Would you like to add:" + Environment.NewLine
                 + "  a [f]reeform message (e.g. instructions, password, or recovery codes)," + Environment.NewLine
                 + "  a [b]inary file," + Environment.NewLine
                 + "  a [t]ext file," + Environment.NewLine
                 + "  a [k]ey pair (or private bits only)," + Environment.NewLine
                 + (items.Any() ? "  or [c]ancel?" : "  or [q]uit?")))
            {
                case 'f':
                    Console.WriteLine();
                    AddFreeformMessage();
                    break;
                case 'b':
                case 't':
                case 'k':
                    throw new NotImplementedException();
                case 'q':
                    quit = true;
                    break;
            }
        }

        private void AddFreeformMessage()
        {
            var message = new StringBuilder();
            Console.WriteLine();
            ConsoleUtils.EditMultilineMessage(message);

            FinishAddingItem(
                BackupContentType.Text,
                fileName: null,
                ThresholdBackup.Utf8NoBom.GetBytes(message.ToString()));
        }

        private void FinishAddingItem(BackupContentType contentType, string fileName, ReadOnlyMemory<byte> content)
        {
            var description = ConsoleUtils.GetRequiredDescription();

            items.Add(new BackupItem(description, contentType, fileName, content));
        }

        private void FinishEditingItem(int index, string newFileName, ReadOnlyMemory<byte> newContent)
        {
            var item = items[index];
            Console.WriteLine("This is the current description: " + item.Description);

            var description = ConsoleUtils.ChooseYesNo("Would you like to change it?")
                ? ConsoleUtils.GetRequiredDescription()
                : item.Description;

            items[index] = new BackupItem(description, item.ContentType, newFileName, newContent);
        }

        private void ListItems()
        {
            Console.WriteLine();

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                Console.Write($"Item {i + 1}: '{item.Description}'");

                switch (item.ContentType)
                {
                    case BackupContentType.Text:
                        Console.Write(", text");
                        break;

                    case BackupContentType.TextFile:
                        Console.Write(", text file " + item.FileName);
                        break;

                    case BackupContentType.BinaryFile:
                        Console.Write(", binary file " + item.FileName);
                        break;

                    case BackupContentType.KeyPair:
                        Console.Write(", key pair");
                        break;

                    case BackupContentType.PrivateKey:
                        Console.Write(", private key");
                        break;
                }

                var kilobytes = Math.Round(item.Content.Length / 1024d, 1);

                Console.WriteLine(kilobytes > 0
                    ? $", {kilobytes} KB"
                    : $", {item.Content.Length} bytes");
            }
        }

        private void EditItem()
        {
            if (!TryChooseItem("edit", out var index)) return;

            var item = items[index];

            switch (item.ContentType)
            {
                case BackupContentType.Text:
                    var message = new StringBuilder(ThresholdBackup.Utf8NoBom.GetString(item.Content.Span));
                    Console.WriteLine();
                    ConsoleUtils.EditMultilineMessage(message);
                    FinishEditingItem(index, newFileName: null, ThresholdBackup.Utf8NoBom.GetBytes(message.ToString()));
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        private void DiscardItem()
        {
            if (TryChooseItem("discard", out var index))
                items.RemoveAt(index);
        }

        private bool TryChooseItem(string verb, out int index)
        {
            if (items.Count < 2)
            {
                index = 0;
                return items.Any();
            }

            ListItems();
            Console.WriteLine();
            Console.Write($"Which item would you like to {verb}? ");

            if (int.TryParse(Console.ReadLine(), out var number) && number >= 1 && number <= items.Count)
            {
                index = number - 1;
                return true;
            }

            Console.WriteLine($"A number between 1 and {items.Count} is required.");
            index = default;
            return false;
        }

        private void PrintBackup()
        {
            Console.WriteLine();
            ConsoleUtils.WriteLineWithWordBreaks(
                "The encryption key will be randomly generated and then split into a number of total parts which are known " +
                "as shares. You get to choose both the total number of shares and also the threshold (the number of shares required " +
                "in order to reconstruct the key and restore this backup).");

            Console.WriteLine();
            ConsoleUtils.WriteLineWithWordBreaks(
                "If you choose a threshold that is too low, it may be too easy for an adversary to decrypt your backup. " +
                "If the threshold is too high, it may be too easy for an adversary or disaster to cause you to lose your " +
                "backup permanently. (There is no way to recover any part of this backup unless you have the exact number " +
                "of shares in hand.)");

            Console.WriteLine();
            var totalParts = ConsoleUtils.GetPositiveInteger("How many total shares would you like to generate?", max: 10_000);

            Console.WriteLine();
            var requiredParts = ConsoleUtils.GetPositiveInteger("How many shares would you like to require in order to recover the backup?", max: totalParts);

            Console.WriteLine();
            Console.WriteLine("What would you like the printed document title to be?");
            Console.WriteLine("For example: Paper backup of Jane’s digital information");

            var documentTitle = ConsoleUtils.GetNonWhitespaceLine("Printed title:");

            ImmutableArray<SharedSecretPart> shares;

            Console.WriteLine();
            using (var pdfStream = SaveAs())
            {
                shares = ThresholdBackup.Save(items.ToImmutableArray(), totalParts, requiredParts, documentTitle, pdfStream);

                Console.WriteLine();
                Console.WriteLine($"Print {totalParts} copies of PDF which as been saved at '{pdfStream.Name}'.");
            }

            Console.WriteLine();
            ConsoleUtils.WriteLineWithWordBreaks(
                $"The PDF is safe to copy and print and even make public. Nothing can be guessed from it (except for the fact " +
                $"that it exists and the general size of the data) until it is combined with exactly {requiredParts} of the " +
                $"following shares of the encryption key.");
            Console.WriteLine();
            ConsoleUtils.WriteLineWithWordBreaks(
                "Using a pen, handwrite one unique share on each printed copy in the designated section on the first page. " +
                "To avoid accidental copies or distortions, make sure the ink dries and does not bleed through. Put a dense " +
                "surface directly underneath the page.");

            foreach (var share in shares)
            {
                Console.WriteLine();
                Console.WriteLine(FormatPart(share));
            }
        }

        private static FileStream SaveAs()
        {
            while (true)
            {
                var fileName = ConsoleUtils.GetNonWhitespaceLine("File name for saved PDF:");
                if (!fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    fileName += ".pdf";

                var fullPath = Path.GetFullPath(fileName);
                try
                {
                    return new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Cannot create file '{fullPath}': {ex.Message}");
                }
            }
        }

        private static string FormatPart(SharedSecretPart part)
        {
            var builder = new StringBuilder();

            builder.Append("X: ").Append(part.X).AppendLine();
            builder.Append("Y: ");

            var span = part.Y.Span;
            for (var i = 0; i < span.Length; i++)
            {
                if (i != 0 && i % 4 == 0)
                {
                    if (i % 12 == 0)
                        builder.AppendLine().Append("   ");
                    else
                        builder.Append('-');
                }

                builder.Append(span[i].ToString("X2"));
            }

            return builder.ToString();
        }
    }
}
