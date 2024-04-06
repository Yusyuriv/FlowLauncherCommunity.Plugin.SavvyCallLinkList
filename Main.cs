using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Flow.Launcher.Plugin;

namespace FlowLauncherCommunity.Plugin.SavvyCalLinkList;

public class Main : IPlugin, IDisposable, IContextMenu, IReloadable, ISettingProvider {
    private const string IcoPath = "icon.png";

    private const string BaseUrl = "https://savvycal.com";
    private const string ApiToggleUrl = "https://api.savvycal.com/v1/links/{0}/toggle";
    private const string ApiUrl = "https://api.savvycal.com/v1/links?limit=100";

    private const int UpdateIntervalInMinutes = 5;

    private readonly HttpClient _client = new();
    private List<SavvyLink> _links = new();
    private Timer? _timer;
    private PluginInitContext _context = null!;
    private Settings _settings = null!;

    public void Init(PluginInitContext context) {
        _context = context;
        _timer = new Timer(
            _ => UpdateData(),
            null,
            TimeSpan.FromMinutes(UpdateIntervalInMinutes).Milliseconds,
            TimeSpan.FromMinutes(UpdateIntervalInMinutes).Milliseconds
        );
        _settings = _context.API.LoadSettingJsonStorage<Settings>();
        _settings.PropertyChanged += Settings_PropertyChanged;
        UpdateAuthorizationHeader();
    }

    private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs args) {
        if (args.PropertyName == nameof(Settings.Token)) UpdateAuthorizationHeader();
    }

    private void UpdateAuthorizationHeader() {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.Token);
        UpdateData();
    }

    public List<Result> Query(Query query) {
        if (string.IsNullOrEmpty(query.Search))
            return _links
                .Select(v => new Result {
                    Title = v.Name,
                    SubTitle = v.PrivateName,
                    IcoPath = IcoPath,
                    ContextData = v,
                    Action = _ => {
                        _context.API.CopyToClipboard(v.Link);
                        return true;
                    }
                })
                .ToList();

        return _links
            .Where(v =>
                _context.API.FuzzySearch(query.Search, v.Name).IsSearchPrecisionScoreMet() ||
                _context.API.FuzzySearch(query.Search, v.PrivateName).IsSearchPrecisionScoreMet()
            )
            .Select(v => new Result {
                Title = v.Name,
                SubTitle = v.PrivateName,
                IcoPath = IcoPath,
                ContextData = v,
                Action = _ => {
                    _context.API.CopyToClipboard(v.Link);
                    return true;
                }
            })
            .ToList();
    }

    private async Task UpdateData() {
        if (string.IsNullOrWhiteSpace(_settings.Token)) {
            _links = new List<SavvyLink>();
            return;
        }
        var response = await _client.GetAsync(ApiUrl);
        if (!response.IsSuccessStatusCode) return;

        var content = await response.Content.ReadAsStringAsync();

        var apiResponse = JsonSerializer.Deserialize<ApiResponse>(content, new JsonSerializerOptions {
            PropertyNameCaseInsensitive = true
        });
        if (apiResponse is null) return;

        _links = apiResponse
            .Entries
            .Select(v => new SavvyLink(
                v.Id,
                v.Name,
                v.PrivateName ?? "",
                $"{BaseUrl}/{v.Scope.Slug}/{v.Slug}"
            ))
            .ToList();
    }

    public void Dispose() {
        _settings.PropertyChanged -= Settings_PropertyChanged;
        _client.Dispose();
        _timer?.Dispose();
    }

    public List<Result> LoadContextMenus(Result selectedResult) {
        if (selectedResult.ContextData is not SavvyLink link) return new List<Result>();

        var name = link.Name;
        if (!string.IsNullOrEmpty(link.PrivateName)) name = $"{link.Name} ({link.PrivateName})";

        return new List<Result> {
            new() {
                Title = "Copy link",
                SubTitle = name,
                IcoPath = IcoPath,
                Action = _ => {
                    _context.API.CopyToClipboard(link.Link);
                    return true;
                }
            },
            new() {
                Title = "Open link in browser",
                SubTitle = name,
                IcoPath = IcoPath,
                Action = _ => {
                    _context.API.OpenUrl(link.Link);
                    return true;
                }
            },
            new() {
                Title = "Toggle availability",
                SubTitle = name,
                IcoPath = IcoPath,
                AsyncAction = async _ => {
                    var response = await _client.PostAsync(string.Format(ApiToggleUrl, link.Id), null);
                    _context.API.ShowMsg(response.IsSuccessStatusCode switch {
                        true => "Successfully toggled availability",
                        false => "Failed to toggle availability"
                    });
                    return true;
                }
            }
        };
    }

    public void ReloadData() {
        UpdateData();
    }

    public Control CreateSettingPanel() {
        return new SettingsView(_settings);
    }
}