using GRIF.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GRIF.Services
{
    public interface IPreviewDocumentViewModelFactory
    {
        Task<ViewModel_PreviewDocument> CreateAsync(object documentData, TemplateService templateService, IDataBaseLoader dataBaseLoader);
    }

    public class PreviewDocumentViewModelFactory : IPreviewDocumentViewModelFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public PreviewDocumentViewModelFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<ViewModel_PreviewDocument> CreateAsync(object documentData, TemplateService templateService, IDataBaseLoader dataBaseLoader)
        {
            var documentFactory = _serviceProvider.GetRequiredService<IDocumentFactory>();
            var viewModel = new ViewModel_PreviewDocument(documentData, templateService, documentFactory, dataBaseLoader);
            await viewModel.InitializationTask;
            return viewModel;
        }
    }
}
