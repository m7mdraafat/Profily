using Microsoft.Extensions.Options;
using Octokit;
using Profily.Core.Interfaces;
using Profily.Infrastructure.Settings;

namespace Profily.Infrastructure.Services;

public sealed class GitHubAuthService : IGitHubAuthService
{
    private readonly GitHubSettings _settings;
    private static readonly string[] _scopes = ["read:user", "user:email", "repo"];

    public GitHubAuthService(IOptions<GitHubSettings> settings)
    {
        _settings = settings.Value;
    }

    public string GetAuthorizationUrl(string state)
    {
        var request = new OauthLoginRequest(_settings.ClientId)
        {
            State = state
        };

        foreach (var scope in _scopes)
        {
            request.Scopes.Add(scope);
        }

        var client = new GitHubClient(new ProductHeaderValue("Profily"));
        return client.Oauth.GetGitHubLoginUrl(request).ToString();
    }

    public async Task<string> ExchangeCodeForTokenAsync(string code, CancellationToken ct = default)
    {
        var client = new GitHubClient(new ProductHeaderValue("Profily"));

        var tokenRequest = new OauthTokenRequest(_settings.ClientId, _settings.ClientSecret, code);
        var token = await client.Oauth.CreateAccessToken(tokenRequest);

        if (string.IsNullOrEmpty(token.AccessToken))
        {
            throw new InvalidOperationException("GitHub OAuth token exchange failed.");
        }

        return token.AccessToken;
    }

    public async Task<GitHubUserProfile> GetUserProfileAsync(string accessToken, CancellationToken ct = default)
    {
        var client = new GitHubClient(new ProductHeaderValue("Profily"))
        {
            Credentials = new Credentials(accessToken)
        };

        var user = await client.User.Current();

        // Fetch primary email (user:email scope required)
        var email = user.Email;
        if (string.IsNullOrEmpty(email))
        {
            var emails = await client.User.Email.GetAll();
            email = emails.FirstOrDefault(e => e.Primary)?.Email;
        }

        return new GitHubUserProfile
        {
            Id = user.Id,
            Login = user.Login,
            Name = user.Name,
            AvatarUrl = user.AvatarUrl,
            Bio = user.Bio,
            Location = user.Location,
            Company = user.Company,
            Email = email,
            HtmlUrl = user.HtmlUrl,
            PublicRepos = user.PublicRepos,
            Followers = user.Followers,
        };
    }
}