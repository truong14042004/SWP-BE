using System.Text.RegularExpressions;

namespace SWP_BE.Services;

public sealed class SkillExtractor : ISkillExtractor
{
    private static readonly string[] Keywords =
    [
        // Languages
        "Python", "Java", "JavaScript", "TypeScript", "C#", "C++", "Go", "Golang",
        "Rust", "PHP", "Ruby", "Kotlin", "Swift", "Dart", "Scala", "Objective-C",
        // Frontend
        "React", "Vue", "Vue.js", "Angular", "Next.js", "Nuxt", "Svelte", "jQuery",
        "HTML", "CSS", "Tailwind", "TailwindCSS", "Sass", "SCSS", "Bootstrap",
        "Webpack", "Vite", "Redux", "Zustand",
        // Backend / Frameworks
        "Node.js", "NodeJS", "Express", "Express.js", "NestJS", "Nest.js",
        "Django", "Flask", "FastAPI", "Spring", "Spring Boot", "ASP.NET", ".NET",
        ".NET Core", "Laravel", "Ruby on Rails", "Rails", "CodeIgniter",
        // Mobile
        "Android", "iOS", "React Native", "Flutter", "Xamarin", "Jetpack Compose",
        "SwiftUI",
        // Databases
        "PostgreSQL", "Postgres", "MySQL", "MongoDB", "Redis", "SQL Server",
        "MSSQL", "Oracle", "Elasticsearch", "DynamoDB", "Cassandra", "MariaDB",
        "SQLite", "Firestore", "Firebase",
        // DevOps / Cloud
        "Docker", "Kubernetes", "K8s", "AWS", "Azure", "GCP", "Google Cloud",
        "Terraform", "Ansible", "Jenkins", "GitLab CI", "GitHub Actions",
        "CircleCI", "Linux", "Nginx", "Apache", "Bash", "PowerShell",
        // Data / AI
        "TensorFlow", "PyTorch", "Pandas", "NumPy", "Spark", "Hadoop", "Kafka",
        "Airflow", "Snowflake", "BigQuery", "dbt", "LangChain", "LlamaIndex",
        "OpenAI", "Gemini", "GPT", "LLM",
        // QA / Testing
        "Selenium", "Cypress", "Playwright", "JUnit", "xUnit", "NUnit", "Jest",
        "Mocha", "PyTest", "Postman",
        // Tools / Concepts
        "Git", "GitHub", "GitLab", "Bitbucket", "REST", "GraphQL", "gRPC",
        "WebSocket", "OAuth", "JWT", "CI/CD", "Microservices", "Agile", "Scrum",
        "Kanban", "DDD", "TDD", "Clean Architecture",
    ];

    private static readonly Regex[] CompiledPatterns = Keywords
        .Select(BuildPattern)
        .ToArray();

    public IReadOnlyList<ExtractedKeyword> Extract(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<ExtractedKeyword>();
        }

        var results = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < Keywords.Length; i++)
        {
            var matches = CompiledPatterns[i].Matches(text);
            if (matches.Count == 0)
            {
                continue;
            }

            var canonical = NormalizeCanonical(Keywords[i]);
            results.TryGetValue(canonical, out var current);
            results[canonical] = current + matches.Count;
        }

        return results
            .Select(item => new ExtractedKeyword(item.Key, item.Value))
            .OrderByDescending(item => item.Count)
            .ToList();
    }

    private static Regex BuildPattern(string keyword)
    {
        var escaped = Regex.Escape(keyword);
        var hasWordStart = char.IsLetterOrDigit(keyword[0]);
        var hasWordEnd = char.IsLetterOrDigit(keyword[^1]);
        var pattern = $"{(hasWordStart ? "(?<![A-Za-z0-9_])" : string.Empty)}{escaped}{(hasWordEnd ? "(?![A-Za-z0-9_])" : string.Empty)}";
        return new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    private static string NormalizeCanonical(string keyword)
    {
        return keyword.ToLowerInvariant() switch
        {
            "nodejs" => "Node.js",
            "node.js" => "Node.js",
            "vue.js" => "Vue",
            "nest.js" => "NestJS",
            "express.js" => "Express",
            "golang" => "Go",
            "postgres" => "PostgreSQL",
            "mssql" => "SQL Server",
            "k8s" => "Kubernetes",
            "google cloud" => "GCP",
            "tailwindcss" => "Tailwind",
            "scss" => "Sass",
            _ => keyword,
        };
    }
}
