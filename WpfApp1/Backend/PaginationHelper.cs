
namespace WpfApp1
{
    public class PaginationHelper<T>
    {
        private List<T> _allItems = new();
        private List<T> _filteredItems = new();
        private Func<T, bool>? _currentFilter;
        private Func<IEnumerable<T>, IOrderedEnumerable<T>>? _currentSort;
        
        public int CurrentPage { get; private set; } = 1;
        public int PageSize { get; private set; } = 15;
        public int TotalPages => (int)Math.Ceiling((double)_filteredItems.Count / PageSize);
        public int TotalItems => _filteredItems.Count;
        
        public event Action? PageChanged;
        
        public void SetData(List<T> items)
        {
            _allItems = items ?? new List<T>();
            ApplyFilterAndSort();
            CurrentPage = 1;
            PageChanged?.Invoke();
        }
        
        public void SetFilter(Func<T, bool>? filter)
        {
            _currentFilter = filter;
            ApplyFilterAndSort();
            CurrentPage = 1;
            PageChanged?.Invoke();
        }
        
        public void SetSort(Func<IEnumerable<T>, IOrderedEnumerable<T>>? sortFunc)
        {
            _currentSort = sortFunc;
            ApplyFilterAndSort();
            CurrentPage = 1;
            PageChanged?.Invoke();
        }
        
        private void ApplyFilterAndSort()
        {
            IEnumerable<T> items = _allItems;
            
            // Apply filter first
            if (_currentFilter != null)
            {
                items = items.Where(_currentFilter);
            }
            
            // Apply sort second
            if (_currentSort != null)
            {
                items = _currentSort(items);
            }
            
            _filteredItems = items.ToList();
        }
        
        public List<T> GetCurrentPageItems()
        {
            if (_filteredItems.Count == 0) return new List<T>();
            
            int skip = (CurrentPage - 1) * PageSize;
            return _filteredItems.Skip(skip).Take(PageSize).ToList();
        }
        
        public bool GoToPage(int page)
        {
            if (page < 1 || page > TotalPages) return false;
            
            CurrentPage = page;
            PageChanged?.Invoke();
            return true;
        }
        
        public bool NextPage()
        {
            return GoToPage(CurrentPage + 1);
        }
        
        public bool PreviousPage()
        {
            return GoToPage(CurrentPage - 1);
        }
        
        public bool FirstPage()
        {
            return GoToPage(1);
        }
        
        public bool LastPage()
        {
            return GoToPage(TotalPages);
        }
        
        public void SetPageSize(int pageSize)
        {
            if (pageSize < 1) return;
            
            PageSize = pageSize;
            CurrentPage = 1;
            PageChanged?.Invoke();
        }
        
        public string GetPageInfo()
        {
            if (TotalPages == 0) return "0 / 0";
            return $"{CurrentPage} / {TotalPages}";
        }
        
        public bool CanGoNext => CurrentPage < TotalPages;
        public bool CanGoPrevious => CurrentPage > 1;
        public bool CanGoFirst => CurrentPage > 1;
        public bool CanGoLast => CurrentPage < TotalPages;
    }
}
