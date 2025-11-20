using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AcuPuntos.ViewModels
{
    /// <summary>
    /// ViewModel base con soporte genérico para filtrado y búsqueda.
    /// Reduce duplicación de código en ViewModels con listas filtrables.
    /// </summary>
    /// <typeparam name="T">Tipo de elemento en la colección</typeparam>
    public partial class FilterableViewModel<T> : BaseViewModel
    {
        [ObservableProperty]
        private ObservableCollection<T> items;

        [ObservableProperty]
        private ObservableCollection<T> filteredItems;

        [ObservableProperty]
        private string searchText = "";

        /// <summary>
        /// Función de filtrado personalizada. Debe ser establecida por la clase derivada.
        /// </summary>
        protected Func<T, string, bool>? SearchFilter { get; set; }

        /// <summary>
        /// Filtros adicionales que pueden ser aplicados. Debe ser establecida por la clase derivada.
        /// </summary>
        protected Func<IEnumerable<T>, IEnumerable<T>>? AdditionalFilters { get; set; }

        public FilterableViewModel()
        {
            Items = new ObservableCollection<T>();
            FilteredItems = new ObservableCollection<T>();
        }

        partial void OnSearchTextChanged(string value)
        {
            ApplyFilters();
        }

        /// <summary>
        /// Aplica todos los filtros configurados a la colección de items.
        /// </summary>
        protected virtual void ApplyFilters()
        {
            FilteredItems.Clear();

            var filtered = Items.AsEnumerable();

            // Aplicar filtro de búsqueda si está configurado
            if (!string.IsNullOrWhiteSpace(SearchText) && SearchFilter != null)
            {
                filtered = filtered.Where(item => SearchFilter(item, SearchText.ToLower()));
            }

            // Aplicar filtros adicionales si están configurados
            if (AdditionalFilters != null)
            {
                filtered = AdditionalFilters(filtered);
            }

            foreach (var item in filtered)
            {
                FilteredItems.Add(item);
            }
        }

        /// <summary>
        /// Reemplaza todos los items y aplica filtros.
        /// </summary>
        protected void SetItems(IEnumerable<T> newItems)
        {
            Items.Clear();
            foreach (var item in newItems)
            {
                Items.Add(item);
            }
            ApplyFilters();
        }

        /// <summary>
        /// Agrega un item y vuelve a aplicar filtros.
        /// </summary>
        protected void AddItem(T item)
        {
            Items.Add(item);
            ApplyFilters();
        }

        /// <summary>
        /// Remueve un item y vuelve a aplicar filtros.
        /// </summary>
        protected void RemoveItem(T item)
        {
            Items.Remove(item);
            ApplyFilters();
        }

        /// <summary>
        /// Limpia todos los items.
        /// </summary>
        protected void ClearItems()
        {
            Items.Clear();
            FilteredItems.Clear();
        }
    }
}
