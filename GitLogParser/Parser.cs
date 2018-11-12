using System;
using System.Collections.Generic;
using System.IO;

namespace GitLogParser
{
    public class Parser
    {
        private readonly Predicate<string> _filter = (row) => true;
        private readonly Func<string, string> _handler = (row) => row;

        public Parser() { }
        public Parser(Predicate<string> filterPredicate)
        {
            _filter = filterPredicate;
        }
        public Parser(Predicate<string> filterPredicate, Func<string, string> rowHandler)
        {
            _filter = filterPredicate;
            _handler = rowHandler;
        }

        public IEnumerable<Commit> Parse(string logPath)
        {
            if (!File.Exists(logPath))
            {
                throw new FileNotFoundException("Specified path does not exists.");
            }

            List<List<string>> rows = new List<List<string>>();
            bool isFirstRowHandled = false;

            using (var stream = new StreamReader(logPath))
            {
                var row = string.Empty;

                while (!stream.EndOfStream)
                {
                    if (!isFirstRowHandled)
                    {
                        row = stream.ReadLine();
                    }

                    if (row == null || !row.StartsWith("commit")) continue;

                    var currentCommit = new List<string> { row };

                    row = stream.ReadLine();
                    while (row != null && !row.StartsWith("commit"))
                    {
                        row = _handler(row);

                        if (_filter(row))
                        {
                            currentCommit.Add(row);
                        }

                        row = stream.ReadLine();
                    }

                    rows.Add(currentCommit);
                    isFirstRowHandled = true;
                }
            }

            rows = rows.FindAll(commit => commit.Count > 3);

            List<Commit> commits = new List<Commit>();

            foreach (var commit in rows)
            {
                // remove "commit " substring and get full-hash
                string fullHash = commit[0].Replace("commit ", string.Empty).Split(' ')[0];
                Commit finalCommit = new Commit
                {
                    Hash = fullHash.Substring(0, 7),
                    FullHash = fullHash,
                };

                string message = string.Empty;
                for (int i = 3; i < commit.Count; i++)
                {
                    message += $"{commit[i]}.";
                }

                finalCommit.Message = message;

                commits.Add(finalCommit);
            }

            return commits;
        }
    }
}
