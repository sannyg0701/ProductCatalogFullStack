import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, takeUntil } from 'rxjs/operators';
import { ProductService } from '../../services/product.service';
import { CategoryService } from '../../services/category.service';
import { Product, ProductSearchParams } from '../../models/product.model';
import { Category } from '../../models/category.model';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './product-list.component.html'
})
export class ProductListComponent implements OnInit, OnDestroy {
  private readonly productService = inject(ProductService);
  private readonly categoryService = inject(CategoryService);
  private readonly destroy$ = new Subject<void>();
  private readonly searchTerms$ = new Subject<string>();

  products: Product[] = [];
  categories: Category[] = [];
  errorMessage: string | null = null;
  isLoading = true;

  // Search filters
  searchTerm = '';
  selectedCategoryId: number | null = null;
  minPrice: number | null = null;
  maxPrice: number | null = null;
  inStockOnly = false;

  // Sorting
  sortBy = 'name';
  sortOrder = 'asc';

  // Pagination
  totalCount = 0;
  pageNumber = 1;
  pageSize = 10;
  totalPages = 0;

  ngOnInit(): void {
    this.loadCategories();
    this.setupLiveSearch();
    this.search();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private setupLiveSearch(): void {
    this.searchTerms$
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        takeUntil(this.destroy$)
      )
      .subscribe(() => {
        this.pageNumber = 1;
        this.search();
      });
  }

  onSearchTermChange(term: string): void {
    this.searchTerm = term;
    this.searchTerms$.next(term);
  }

  private loadCategories(): void {
    this.categoryService.getCategories().subscribe({
      next: (data) => this.categories = data,
      error: () => console.error('Failed to load categories')
    });
  }

  search(): void {
    this.isLoading = true;
    this.errorMessage = null;

    const params: ProductSearchParams = {
      searchTerm: this.searchTerm || undefined,
      categoryId: this.selectedCategoryId || undefined,
      minPrice: this.minPrice ?? undefined,
      maxPrice: this.maxPrice ?? undefined,
      inStock: this.inStockOnly ? true : undefined,
      sortBy: this.sortBy,
      sortOrder: this.sortOrder,
      pageNumber: this.pageNumber,
      pageSize: this.pageSize
    };

    this.productService.searchProducts(params).subscribe({
      next: (result) => {
        this.products = result.items;
        this.totalCount = result.totalCount;
        this.totalPages = result.totalPages;
        this.isLoading = false;
      },
      error: (err) => {
        this.errorMessage = 'Failed to load products. Please ensure the API is running.';
        this.isLoading = false;
        console.error('Error loading products:', err);
      }
    });
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.selectedCategoryId = null;
    this.minPrice = null;
    this.maxPrice = null;
    this.inStockOnly = false;
    this.sortBy = 'name';
    this.sortOrder = 'asc';
    this.pageNumber = 1;
    this.search();
  }

  sort(column: string): void {
    if (this.sortBy === column) {
      this.sortOrder = this.sortOrder === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortBy = column;
      this.sortOrder = 'asc';
    }
    this.pageNumber = 1;
    this.search();
  }

  getSortIndicator(column: string): string {
    if (this.sortBy !== column) return '';
    return this.sortOrder === 'asc' ? ' ▲' : ' ▼';
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages) return;
    this.pageNumber = page;
    this.search();
  }

  deleteProduct(product: Product): void {
    if (!confirm(`Are you sure you want to delete "${product.name}"?`)) {
      return;
    }

    this.productService.deleteProduct(product.id).subscribe({
      next: () => {
        this.products = this.products.filter(p => p.id !== product.id);
        this.totalCount--;
      },
      error: (err) => {
        this.errorMessage = 'Failed to delete product.';
        console.error('Error deleting product:', err);
      }
    });
  }
}
