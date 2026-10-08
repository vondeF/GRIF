using GRIF.Entities.BasedOnConfig;
using GRIF.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GRIF.Services
{
    public interface ITemplateServiceFactory
    {
        TemplateService Create(object documentConfigObject);
    }

    public class TemplateServiceFactory : ITemplateServiceFactory
    {
        public TemplateService Create(object documentConfigObject)
        {
            return documentConfigObject switch
            {
                ModelDocumentConfigData gost => new TemplateService("Шаблоны Профили модели", gost.Name, new GostDocumentTemplateValidator(gost)),
                ExchangeDocumentConfigData exchange => new TemplateService("Шаблоны Профили обмена", exchange.Name, new ExchangeDocumentTemplateValidator()),
                ExchangeDocumentDbData exchange => new TemplateService("Шаблоны Профили обмена", "", new ExchangeDocumentTemplateValidator()),
                _ => throw new ArgumentException($"Неизвестный тип документа: {documentConfigObject.GetType().Name}")
            };
        }
    }
}
