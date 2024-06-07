using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using AiTestLibrary.Classes;
using AiTestLibrary.Interfaces;
using GemBox.Document;
using Microsoft.Win32;
using SimpleIntegrationAi.Domain.Models;
using SimpleIntegrationAi.Domain.Services;
using SimpleIntegrationAi.WPF.Commands;

namespace SimpleIntegrationAi.WPF.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly AsyncCommand _loadFileCommand;
    private readonly IYandexGpt _yandexGpt = new YandexGpt();
    private readonly IResponseParser _responseParser = new ResponseParser();
    private ObservableCollection<MessageItem> _items;
    private readonly string foledrId = "b1gphb1c693npe94nmrv";

    private readonly string iamtoken =
        "t1.9euelZqWnZWKm4qZl5bKk4qMy5jIku3rnpWam46WzZKSlMaVjsycjZCZksjl8_cKH3RM-e9xQHwh_d3z90pNcUz573FAfCH9zef1656VmprOipLJxpSSlciOlMrMjMyN7_zF656VmprOipLJxpSSlciOlMrMjMyN.03noIyK_w1so2oFjDdKxohFTXnNoYBtLS_j70h7P4nt8tPukrTKjqKGZWADqf50gquIkxonrWzWxqsGUvIDYDQ";

    public MainWindowViewModel()
    {
        _loadFileCommand = new AsyncCommand(LoadFile);
    }

    public ICommand LoadFileCommand => _loadFileCommand;

    public ObservableCollection<MessageItem> Items
    {
        get => _items;
        set => SetField(ref _items, value);
    }

    private async Task LoadFile()
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Text Files (*.txt)|*.txt|Word Documents (*.docx)|*.docx|All Files (*.*)|*.*"
        };
        if (openFileDialog.ShowDialog() == true)
        {
            var filePath = openFileDialog.FileName;
            var fileExtension = Path.GetExtension(filePath);

            if (fileExtension == ".docx")
            {
                ComponentInfo.SetLicense("FREE-LIMITED-KEY");
                var document = DocumentModel.Load(filePath);
                var messageViewModel = new MessageItem(document.Content.ToString());
                Items = [messageViewModel];
            }

            else if (fileExtension == ".txt")
            {
                var messageViewModel = new MessageItem(File.ReadAllText(filePath));
                Items = [messageViewModel];
            }
        }
        var result = MessageBox.Show("Хотите чтобы ИИ обработал данные файла?", "Повторить?", MessageBoxButton.YesNo);

        if (result == MessageBoxResult.Yes) await InitializeAsync();
    }

    public async Task InitializeAsync()
    {
        var result = string.Join(" ", Items.Select(i => i.Message));

        var messageCollection =
            await _yandexGpt.Request($"{result} Выбери из списка сущности и выведи их без описания.", iamtoken,
                foledrId);

        var parsedMessage = _responseParser.GetMessageAsync(messageCollection);

        Items.Clear();

        foreach (var msg in parsedMessage)
            Items.Add(new MessageItem(msg));
        
    }
}