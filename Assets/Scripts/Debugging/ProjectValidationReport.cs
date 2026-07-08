using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class ProjectValidationReport {
    public int errorCount;
    public int warningCount;
    public List<ProjectValidationIssue> issues = new List<ProjectValidationIssue>();

    public bool HasErrors => errorCount > 0;

    public void Error(string message, string context = null) {
        Add(ProjectValidationSeverity.Error, message, context);
    }

    public void Warning(string message, string context = null) {
        Add(ProjectValidationSeverity.Warning, message, context);
    }

    public void Info(string message, string context = null) {
        Add(ProjectValidationSeverity.Info, message, context);
    }

    void Add(ProjectValidationSeverity severity, string message, string context) {
        issues.Add(new ProjectValidationIssue() {
            severity = severity,
            message = message,
            context = context
        });

        if(severity == ProjectValidationSeverity.Error) errorCount++;
        if(severity == ProjectValidationSeverity.Warning) warningCount++;
    }

    public string BuildSummary() {
        return $"Validation finished. Errors={errorCount}, Warnings={warningCount}, Issues={issues.Count}";
    }

    public IEnumerable<ProjectValidationIssue> ImportantIssues() {
        return issues.Where(i => i.severity != ProjectValidationSeverity.Info);
    }
}

[Serializable]
public class ProjectValidationIssue {
    public ProjectValidationSeverity severity;
    public string message;
    public string context;
}

public enum ProjectValidationSeverity {
    Info,
    Warning,
    Error
}
