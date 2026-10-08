using GRIF.Entities.BasedOnConfig;
using GRIF.Entities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GRIF.Services
{
    public interface IDataBaseLoaderFactory
    {
        IDataBaseLoader CreateLoader(object configData);
    }

    public class DataBaseLoaderFactory : IDataBaseLoaderFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public DataBaseLoaderFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IDataBaseLoader CreateLoader(object data)
        {
            IDataBaseLoader loader = data switch
            {
                ModelDocumentConfigData => _serviceProvider.GetRequiredService<ModelDocumentDataBaseLoader>(),
                //ExchangeDocumentConfigData => _serviceProvider.GetRequiredService<ExchangeDocumentDataBaseLoader>(),
                ExchangeDocumentDbData => _serviceProvider.GetRequiredService<ExchangeDocumentDataBaseLoader>(),
                _ => throw new NotSupportedException($"Не найден loader для {data.GetType()}")
            };

            return loader;
        }
    }
}
