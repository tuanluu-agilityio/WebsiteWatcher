using HtmlAgilityPack;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Sql;
using Microsoft.Extensions.Logging;

namespace WebsiteWatcher.Functions;

public class Snapshot(ILogger<Snapshot> logger)
{
    [Function(nameof(Snapshot))]
    [SqlOutput("dbo.Snapshots", "WebsiteWatcher")]
    public SnapshotRecord? Run(
            [SqlTrigger("[dbo].[Websites]", "WebsiteWatcher")] IReadOnlyList<SqlChange<Website>> changes)
    {
        foreach (var change in changes)
        {
            logger.LogInformation($"Change Type: {change.Operation}");
            logger.LogInformation($"Website Id: {change.Item.Id} Website Url: {change.Item.Url}");

            if (change.Operation != SqlChangeOperation.Insert)
            {
                continue;
            }

            HtmlWeb web = new();
            HtmlDocument doc = web.Load(change.Item.Url);
            var divWithContent = doc.DocumentNode.SelectSingleNode(change.Item.XPathExpression);
            var content = divWithContent != null ? divWithContent.InnerText.Trim() : "No content";

            logger.LogInformation($"Content: {content}");

            return new SnapshotRecord(change.Item.Id, content);
        }

        return null;
    }
}

public record SnapshotRecord(Guid Id, string Content);