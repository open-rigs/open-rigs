using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRigs.Logging.Tests
{
    [TestClass]
    public class LogDataTests
    {
        const string WorkflowFileName = "LogDataTest.bonsai";
        const string SubjectId = "TestSubject";
        const string SessionId = "001";
        const int ExpectedSampleCount = 5;

        static readonly string[] ExpectedHeader = { "Seconds", "Value.X", "Value.Y", "Value.Z" };

        string logRoot;

        [TestInitialize]
        public void TestInitialize()
        {
            // Log outside the repository so that a failed run never leaves files in the working tree.
            logRoot = Path.Combine(Path.GetTempPath(), "OpenRigs.Logging.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(logRoot);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            if (logRoot == null || !Directory.Exists(logRoot))
            {
                return;
            }

            try
            {
                Directory.Delete(logRoot, recursive: true);
            }
            catch (IOException)
            {
                // Leaving a stray temp directory behind should not fail the run.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        [TestMethod]
        public void LogData_TimestampedPoint3d_WritesOneSamplePerRow()
        {
            var result = BonsaiWorkflowRunner.Run(WorkflowFileName, new Dictionary<string, string>
            {
                { "SubjectId", SubjectId },
                { "SessionId", SessionId },
                { "Path", logRoot }
            });

            // The log file name embeds a timestamp, so search for it rather than reconstructing the path.
            var logFiles = Directory.GetFiles(logRoot, "*.csv", SearchOption.AllDirectories);
            Assert.AreEqual(
                1,
                logFiles.Length,
                $"Expected the workflow to write exactly one CSV log file under '{logRoot}'.{result.Describe()}");

            var lines = File.ReadAllLines(logFiles[0]);
            Assert.AreEqual(
                ExpectedSampleCount + 1,
                lines.Length,
                $"Expected a single header row followed by {ExpectedSampleCount} samples in " +
                $"'{logFiles[0]}'.{result.Describe()}");

            CollectionAssert.AreEqual(
                ExpectedHeader,
                lines[0].Split(','),
                $"Unexpected header row in '{logFiles[0]}'.{result.Describe()}");

            for (var sample = 0; sample < ExpectedSampleCount; sample++)
            {
                var line = lines[sample + 1];
                var columns = line.Split(',');
                Assert.AreEqual(
                    ExpectedHeader.Length,
                    columns.Length,
                    $"Unexpected column count in sample {sample} ('{line}').{result.Describe()}");

                Assert.IsTrue(
                    double.TryParse(columns[0], NumberStyles.Float, CultureInfo.InvariantCulture, out _),
                    $"Expected '{ExpectedHeader[0]}' in sample {sample} to be a number but found " +
                    $"'{columns[0]}'.{result.Describe()}");

                // The workflow maps the element index onto all three axes of the logged point.
                for (var axis = 1; axis < ExpectedHeader.Length; axis++)
                {
                    Assert.IsTrue(
                        int.TryParse(columns[axis], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value),
                        $"Expected '{ExpectedHeader[axis]}' in sample {sample} to be an integer but found " +
                        $"'{columns[axis]}'.{result.Describe()}");
                    Assert.AreEqual(
                        sample,
                        value,
                        $"Unexpected '{ExpectedHeader[axis]}' in sample {sample}.{result.Describe()}");
                }
            }
        }
    }
}
