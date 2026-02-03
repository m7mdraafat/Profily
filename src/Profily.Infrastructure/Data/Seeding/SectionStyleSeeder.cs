// Profily.Infrastructure/Data/Seeding/SectionStyleSeeder.cs

using Microsoft.Extensions.Logging;
using Profily.Core.Interfaces;
using Profily.Core.Models.Profile;

namespace Profily.Infrastructure.Data.Seeding;

public sealed class SectionStyleSeeder : IDataSeeder
{
    private readonly IDocumentRepository _repository;
    private readonly ILogger<SectionStyleSeeder> _logger;

    public int Priority => 2; // Runs after sections

    public SectionStyleSeeder(IDocumentRepository repository, ILogger<SectionStyleSeeder> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Seeding section styles...");

        var styles = GetAllStyles();
        var existingCount = 0;
        var createdCount = 0;

        foreach (var style in styles)
        {
            var existing = await _repository.GetAsync<SectionStyle>(style.Id, style.UserId, ct);
            if (existing is not null)
            {
                existingCount++;
                continue;
            }

            await _repository.UpsertAsync(style, ct);
            createdCount++;
        }

        _logger.LogInformation(
            "Style seeding complete: {Created} created, {Existing} already existed",
            createdCount, existingCount);
    }

    private static List<SectionStyle> GetAllStyles()
    {
        var styles = new List<SectionStyle>();
        
        styles.AddRange(GetHeaderStyles());
        styles.AddRange(GetBadgeStyles());
        styles.AddRange(GetAboutStyles());
        styles.AddRange(GetSkillsStyles());
        styles.AddRange(GetStatsStyles());
        styles.AddRange(GetStreakStyles());
        styles.AddRange(GetLanguagesStyles());
        styles.AddRange(GetSnakeStyles());
        styles.AddRange(Get3DContribStyles());
        styles.AddRange(GetReposStyles());
        styles.AddRange(GetContactStyles());
        styles.AddRange(GetFooterStyles());

        return styles;
    }

    #region Header Styles

    private static List<SectionStyle> GetHeaderStyles() =>
    [
        new SectionStyle
        {
            Id = "sectionStyle-header-typing-svg",
            UserId = "system",
            SectionId = "section-header",
            Slug = "typing-svg",
            DisplayName = "Typing SVG",
            Description = "Animated typing effect with customizable text",
            MarkdownTemplate = """
                <h1 align="center">
                  <a href="https://git.io/typing-svg">
                    <img src="https://readme-typing-svg.herokuapp.com?font=Fira+Code&weight=600&size=28&pause=1000&color={{ theme.primary | string.replace "#" "" }}&center=true&vCenter=true&width=500&lines={{ display_name | string.replace " " "+" }};{{ tagline | string.replace " " "+" }}" alt="Typing SVG" />
                  </a>
                </h1>
                """
        },
        new SectionStyle
        {
            Id = "sectionStyle-header-capsule-render",
            UserId = "system",
            SectionId = "section-header",
            Slug = "capsule-render",
            DisplayName = "3D Capsule Wave",
            Description = "Animated 3D wave header using capsule-render",
            MarkdownTemplate = """
                <p align="center">
                  <img src="https://capsule-render.vercel.app/api?type=waving&color={{ theme.gradient }}&height=200&section=header&text={{ display_name | string.replace " " "%20" }}&fontSize=50&fontColor={{ theme.text_color | string.replace "#" "" }}&animation=fadeIn" />
                </p>
                
                <p align="center">
                  <em>{{ tagline }}</em>
                </p>
                """
        },
        new SectionStyle
        {
            Id = "sectionStyle-header-simple",
            UserId = "system",
            SectionId = "section-header",
            Slug = "simple-text",
            DisplayName = "Simple Text",
            Description = "Clean text-based header",
            MarkdownTemplate = """
                # Hi there, I'm {{ display_name }} 👋
                
                > {{ tagline }}
                """
        }
    ];

    #endregion

    #region Badge Styles

    private static List<SectionStyle> GetBadgeStyles() =>
    [
        new SectionStyle
        {
            Id = "sectionStyle-badges-shields",
            UserId = "system",
            SectionId = "section-badges",
            Slug = "shields-io",
            DisplayName = "Shields.io Badges",
            Description = "Standard shields.io badge row",
            MarkdownTemplate = """
                <p align="center">
                  <img src="https://komarev.com/ghpvc/?username={{ username }}&label=Profile%20views&color={{ theme.primary | string.replace "#" "" }}&style=flat" alt="Profile views" />
                  <img src="https://img.shields.io/github/followers/{{ username }}?label=Followers&style=social" alt="GitHub followers" />
                </p>
                """
        },
        new SectionStyle
        {
            Id = "sectionStyle-badges-social",
            UserId = "system",
            SectionId = "section-badges",
            Slug = "social-badges",
            DisplayName = "Social Badges",
            Description = "Badges with social links",
            MarkdownTemplate = """
                <p align="center">
                  {{ if social.linkedin }}<a href="{{ social.linkedin }}"><img src="https://img.shields.io/badge/LinkedIn-0077B5?style=for-the-badge&logo=linkedin&logoColor=white" /></a>{{ end }}
                  {{ if social.twitter }}<a href="{{ social.twitter }}"><img src="https://img.shields.io/badge/Twitter-1DA1F2?style=for-the-badge&logo=twitter&logoColor=white" /></a>{{ end }}
                  {{ if social.youtube }}<a href="{{ social.youtube }}"><img src="https://img.shields.io/badge/YouTube-FF0000?style=for-the-badge&logo=youtube&logoColor=white" /></a>{{ end }}
                </p>
                """
        }
    ];

    #endregion

    #region About Styles

    private static List<SectionStyle> GetAboutStyles() =>
    [
        new SectionStyle
        {
            Id = "sectionStyle-about-bullets",
            UserId = "system",
            SectionId = "section-about",
            Slug = "bullet-list",
            DisplayName = "Bullet List",
            Description = "About me in bullet points",
            MarkdownTemplate = """
                ## 🙋‍♂️ About Me
                
                {{ about_me }}
                
                {{ if preferences.show_open_to_work }}
                - 🔭 I'm currently **open to work**
                {{ end }}
                - 📫 How to reach me: {{ social.email }}
                """
        },
        new SectionStyle
        {
            Id = "sectionStyle-about-quote",
            UserId = "system",
            SectionId = "section-about",
            Slug = "quote-style",
            DisplayName = "Quote Style",
            Description = "About me as a blockquote",
            MarkdownTemplate = """
                ## About Me
                
                > {{ about_me }}
                """
        }
    ];

    #endregion

    #region Skills Styles

    private static List<SectionStyle> GetSkillsStyles() =>
    [
        new SectionStyle
        {
            Id = "sectionStyle-skills-icons",
            UserId = "system",
            SectionId = "section-skills",
            Slug = "icons-grid",
            DisplayName = "Icons Grid",
            Description = "Tech stack with icon badges",
            MarkdownTemplate = """
                ## 🛠️ Tech Stack
                
                <p align="center">
                {{ for skill in skills }}
                  <img src="https://img.shields.io/badge/{{ skill }}-{{ theme.primary | string.replace "#" "" }}?style=for-the-badge&logo={{ skill | string.downcase | string.replace " " "" }}&logoColor=white" />
                {{ end }}
                </p>
                """
        },
        new SectionStyle
        {
            Id = "sectionStyle-skills-shields",
            UserId = "system",
            SectionId = "section-skills",
            Slug = "shields-badges",
            DisplayName = "Shield Badges",
            Description = "Flat shield style badges",
            MarkdownTemplate = """
                ## Technologies
                
                {{ for skill in skills }}![{{ skill }}](https://img.shields.io/badge/-{{ skill }}-{{ theme.secondary | string.replace "#" "" }}?style=flat-square&logo={{ skill | string.downcase }}&logoColor=white) {{ end }}
                """
        },
        new SectionStyle
        {
            Id = "sectionStyle-skills-simple",
            UserId = "system",
            SectionId = "section-skills",
            Slug = "simple-list",
            DisplayName = "Simple List",
            Description = "Plain text list of skills",
            MarkdownTemplate = """
                ## Skills
                
                {{ for skill in skills }}- {{ skill }}
                {{ end }}
                """
        }
    ];

    #endregion

    #region Stats Styles

    private static List<SectionStyle> GetStatsStyles() =>
    [
        new SectionStyle
        {
            Id = "sectionStyle-stats-card",
            UserId = "system",
            SectionId = "section-stats",
            Slug = "stats-card",
            DisplayName = "Stats Card",
            Description = "GitHub stats card",
            MarkdownTemplate = """
                ## 📊 GitHub Stats
                
                <p align="center">
                  <img src="https://github-readme-stats.vercel.app/api?username={{ username }}&show_icons=true&theme=transparent&hide_border=true&title_color={{ theme.primary | string.replace "#" "" }}&icon_color={{ theme.primary | string.replace "#" "" }}&text_color={{ theme.text_color | string.replace "#" "" }}&bg_color={{ theme.background | string.replace "#" "" }}" alt="GitHub Stats" />
                </p>
                """
        },
        new SectionStyle
        {
            Id = "sectionStyle-stats-combined",
            UserId = "system",
            SectionId = "section-stats",
            Slug = "combined",
            DisplayName = "Combined Stats",
            Description = "Stats + Languages side by side",
            MarkdownTemplate = """
                ## 📊 GitHub Stats
                
                <p align="center">
                  <img width="49%" src="https://github-readme-stats.vercel.app/api?username={{ username }}&show_icons=true&theme=transparent&hide_border=true&title_color={{ theme.primary | string.replace "#" "" }}" alt="GitHub Stats" />
                  <img width="49%" src="https://github-readme-stats.vercel.app/api/top-langs/?username={{ username }}&layout=compact&theme=transparent&hide_border=true&title_color={{ theme.primary | string.replace "#" "" }}" alt="Top Languages" />
                </p>
                """
        },
        new SectionStyle
        {
            Id = "sectionStyle-stats-trophy",
            UserId = "system",
            SectionId = "section-stats",
            Slug = "trophy",
            DisplayName = "Trophy Display",
            Description = "GitHub profile trophy showcase",
            MarkdownTemplate = """
                ## 🏆 GitHub Trophies
                
                <p align="center">
                  <img src="https://github-profile-trophy.vercel.app/?username={{ username }}&theme=transparent&no-frame=true&no-bg=true&column=7&title_color={{ theme.primary | string.replace "#" "" }}" alt="GitHub Trophies" />
                </p>
                """
        }
    ];

    #endregion

    #region Streak Styles

    private static List<SectionStyle> GetStreakStyles() =>
    [
        new SectionStyle
        {
            Id = "sectionStyle-streak-card",
            UserId = "system",
            SectionId = "section-streak",
            Slug = "streak-card",
            DisplayName = "Streak Card",
            Description = "GitHub contribution streak card",
            MarkdownTemplate = """
                ## 🔥 Streak Stats
                
                <p align="center">
                  <img src="https://github-readme-streak-stats.herokuapp.com/?user={{ username }}&theme=transparent&hide_border=true&ring={{ theme.primary | string.replace "#" "" }}&fire={{ theme.primary | string.replace "#" "" }}&currStreakLabel={{ theme.primary | string.replace "#" "" }}" alt="GitHub Streak" />
                </p>
                """
        }
    ];

    #endregion

    #region Languages Styles

    private static List<SectionStyle> GetLanguagesStyles() =>
    [
        new SectionStyle
        {
            Id = "sectionStyle-languages-compact",
            UserId = "system",
            SectionId = "section-languages",
            Slug = "top-langs-compact",
            DisplayName = "Compact",
            Description = "Compact language breakdown",
            MarkdownTemplate = """
                ## 📝 Top Languages
                
                <p align="center">
                  <img src="https://github-readme-stats.vercel.app/api/top-langs/?username={{ username }}&layout=compact&theme=transparent&hide_border=true&title_color={{ theme.primary | string.replace "#" "" }}" alt="Top Languages" />
                </p>
                """
        },
        new SectionStyle
        {
            Id = "sectionStyle-languages-donut",
            UserId = "system",
            SectionId = "section-languages",
            Slug = "top-langs-donut",
            DisplayName = "Donut Chart",
            Description = "Donut chart language breakdown",
            MarkdownTemplate = """
                ## 📝 Top Languages
                
                <p align="center">
                  <img src="https://github-readme-stats.vercel.app/api/top-langs/?username={{ username }}&layout=donut&theme=transparent&hide_border=true&title_color={{ theme.primary | string.replace "#" "" }}" alt="Top Languages" />
                </p>
                """
        }
    ];

    #endregion

    #region Snake Styles

    private static List<SectionStyle> GetSnakeStyles() =>
    [
        new SectionStyle
        {
            Id = "sectionStyle-snake-green",
            UserId = "system",
            SectionId = "section-snake",
            Slug = "green-snake",
            DisplayName = "Green Snake",
            Description = "Classic green contribution snake",
            MarkdownTemplate = """
                ## 🐍 Contribution Snake
                
                <p align="center">
                  <img src="https://raw.githubusercontent.com/{{ username }}/{{ username }}/output/github-contribution-grid-snake.svg" alt="Snake animation" />
                </p>
                """,
            Workflows =
            [
                new WorkflowDefinition
                {
                    Path = ".github/workflows/snake.yml",
                    Content = """
                        name: Generate Snake
                        
                        on:
                          schedule:
                            - cron: "0 */12 * * *"
                          workflow_dispatch:
                        
                        jobs:
                          build:
                            runs-on: ubuntu-latest
                            steps:
                              - uses: Platane/snk@v3
                                with:
                                  github_user_name: {{ username }}
                                  outputs: |
                                    dist/github-contribution-grid-snake.svg
                                    dist/github-contribution-grid-snake-dark.svg?palette=github-dark
                              
                              - uses: crazy-max/ghaction-github-pages@v3
                                with:
                                  target_branch: output
                                  build_dir: dist
                                env:
                                  GITHUB_TOKEN: ${{ "{{" }} secrets.GITHUB_TOKEN {{ "}}" }}
                        """
                }
            ]
        },
        new SectionStyle
        {
            Id = "sectionStyle-snake-dark",
            UserId = "system",
            SectionId = "section-snake",
            Slug = "dark-snake",
            DisplayName = "Dark Snake",
            Description = "Dark mode contribution snake",
            MarkdownTemplate = """
                ## 🐍 Contribution Snake
                
                <picture>
                  <source media="(prefers-color-scheme: dark)" srcset="https://raw.githubusercontent.com/{{ username }}/{{ username }}/output/github-contribution-grid-snake-dark.svg" />
                  <source media="(prefers-color-scheme: light)" srcset="https://raw.githubusercontent.com/{{ username }}/{{ username }}/output/github-contribution-grid-snake.svg" />
                  <img alt="github-snake" src="https://raw.githubusercontent.com/{{ username }}/{{ username }}/output/github-contribution-grid-snake.svg" />
                </picture>
                """,
            Workflows =
            [
                new WorkflowDefinition
                {
                    Path = ".github/workflows/snake.yml",
                    Content = """
                        name: Generate Snake
                        
                        on:
                          schedule:
                            - cron: "0 */12 * * *"
                          workflow_dispatch:
                        
                        jobs:
                          build:
                            runs-on: ubuntu-latest
                            steps:
                              - uses: Platane/snk@v3
                                with:
                                  github_user_name: {{ username }}
                                  outputs: |
                                    dist/github-contribution-grid-snake.svg
                                    dist/github-contribution-grid-snake-dark.svg?palette=github-dark
                              
                              - uses: crazy-max/ghaction-github-pages@v3
                                with:
                                  target_branch: output
                                  build_dir: dist
                                env:
                                  GITHUB_TOKEN: ${{ "{{" }} secrets.GITHUB_TOKEN {{ "}}" }}
                        """
                }
            ]
        }
    ];

    #endregion

    #region 3D Contrib Styles

    private static List<SectionStyle> Get3DContribStyles() =>
    [
        new SectionStyle
        {
            Id = "sectionStyle-3d-profile",
            UserId = "system",
            SectionId = "section-3d",
            Slug = "3d-profile",
            DisplayName = "3D Profile",
            Description = "3D contribution calendar",
            MarkdownTemplate = """
                ## 📈 3D Contributions
                
                <p align="center">
                  <img src="./profile-3d-contrib/profile-night-rainbow.svg" alt="3D Contributions" />
                </p>
                """,
            Workflows =
            [
                new WorkflowDefinition
                {
                    Path = ".github/workflows/profile-3d.yml",
                    Content = """
                        name: Generate 3D Profile
                        
                        on:
                          schedule:
                            - cron: "0 0 * * *"
                          workflow_dispatch:
                        
                        jobs:
                          build:
                            runs-on: ubuntu-latest
                            steps:
                              - uses: yoshi389111/github-profile-3d-contrib@0.7.1
                                env:
                                  GITHUB_TOKEN: ${{ "{{" }} secrets.GITHUB_TOKEN {{ "}}" }}
                                  USERNAME: {{ username }}
                              
                              - uses: actions/checkout@v4
                              
                              - run: |
                                  git config user.name github-actions
                                  git config user.email github-actions@github.com
                                  git add -A .
                                  git commit -m "Update 3D profile" || exit 0
                                  git push
                        """
                }
            ]
        }
    ];

    #endregion

    #region Repos Styles

    private static List<SectionStyle> GetReposStyles() =>
    [
        new SectionStyle
        {
            Id = "sectionStyle-repos-cards",
            UserId = "system",
            SectionId = "section-repos",
            Slug = "repo-cards",
            DisplayName = "Repo Cards",
            Description = "GitHub repo cards in a grid",
            MarkdownTemplate = """
                ## 📁 Featured Repositories
                
                <p align="center">
                {{ for repo in repositories limit:6 }}
                  <a href="{{ repo.html_url }}">
                    <img src="https://github-readme-stats.vercel.app/api/pin/?username={{ username }}&repo={{ repo.name }}&theme=transparent&hide_border=true&title_color={{ theme.primary | string.replace "#" "" }}" />
                  </a>
                {{ end }}
                </p>
                """
        },
        new SectionStyle
        {
            Id = "sectionStyle-repos-table",
            UserId = "system",
            SectionId = "section-repos",
            Slug = "table-format",
            DisplayName = "Table Format",
            Description = "Repositories in a table",
            MarkdownTemplate = """
                ## 📁 Featured Repositories
                
                | Repository | Description | Stars | Language |
                |------------|-------------|-------|----------|
                {{ for repo in repositories limit:6 }}| [{{ repo.name }}]({{ repo.html_url }}) | {{ repo.description }} | ⭐ {{ repo.stars }} | {{ repo.primary_language }} |
                {{ end }}
                """
        }
    ];

    #endregion

    #region Contact Styles

    private static List<SectionStyle> GetContactStyles() =>
    [
        new SectionStyle
        {
            Id = "sectionStyle-contact-badges",
            UserId = "system",
            SectionId = "section-contact",
            Slug = "badges-row",
            DisplayName = "Badge Row",
            Description = "Contact links as badge row",
            MarkdownTemplate = """
                ## 📫 Connect With Me
                
                <p align="center">
                {{ if social.linkedin }}<a href="{{ social.linkedin }}"><img src="https://img.shields.io/badge/LinkedIn-{{ theme.primary | string.replace "#" "" }}?style=for-the-badge&logo=linkedin&logoColor=white" /></a>{{ end }}
                {{ if social.twitter }}<a href="{{ social.twitter }}"><img src="https://img.shields.io/badge/Twitter-{{ theme.primary | string.replace "#" "" }}?style=for-the-badge&logo=twitter&logoColor=white" /></a>{{ end }}
                {{ if social.email }}<a href="mailto:{{ social.email }}"><img src="https://img.shields.io/badge/Email-{{ theme.primary | string.replace "#" "" }}?style=for-the-badge&logo=gmail&logoColor=white" /></a>{{ end }}
                {{ if social.website }}<a href="{{ social.website }}"><img src="https://img.shields.io/badge/Website-{{ theme.primary | string.replace "#" "" }}?style=for-the-badge&logo=google-chrome&logoColor=white" /></a>{{ end }}
                </p>
                """
        },
        new SectionStyle
        {
            Id = "sectionStyle-contact-icons",
            UserId = "system",
            SectionId = "section-contact",
            Slug = "icons-row",
            DisplayName = "Icons Row",
            Description = "Simple icon links",
            MarkdownTemplate = """
                ## Connect
                
                {{ if social.linkedin }}[![LinkedIn](https://img.shields.io/badge/-LinkedIn-0A66C2?logo=linkedin&logoColor=white)]({{ social.linkedin }}){{ end }}
                {{ if social.twitter }}[![Twitter](https://img.shields.io/badge/-Twitter-1DA1F2?logo=twitter&logoColor=white)]({{ social.twitter }}){{ end }}
                {{ if social.email }}[![Email](https://img.shields.io/badge/-Email-EA4335?logo=gmail&logoColor=white)](mailto:{{ social.email }}){{ end }}
                """
        }
    ];

    #endregion

    #region Footer Styles

    private static List<SectionStyle> GetFooterStyles() =>
    [
        new SectionStyle
        {
            Id = "sectionStyle-footer-wave",
            UserId = "system",
            SectionId = "section-footer",
            Slug = "wave-svg",
            DisplayName = "Wave Footer",
            Description = "Animated wave footer",
            MarkdownTemplate = """
                <p align="center">
                  <img src="https://capsule-render.vercel.app/api?type=waving&color={{ theme.gradient }}&height=100&section=footer" />
                </p>
                """
        },
        new SectionStyle
        {
            Id = "sectionStyle-footer-simple",
            UserId = "system",
            SectionId = "section-footer",
            Slug = "simple-line",
            DisplayName = "Simple Line",
            Description = "Simple horizontal rule footer",
            MarkdownTemplate = """
                ---
                
                <p align="center">
                  <i>Thanks for visiting! ⭐ Star my repos if you find them useful!</i>
                </p>
                """
        }
    ];

    #endregion
}