import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { ProductService } from '../../services/product.service';
import { CategoryService } from '../../services/category.service';
import { Category } from '../../models/category.model';

@Component({
  selector: 'app-product-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './product-form.component.html'
})
export class ProductFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly productService = inject(ProductService);
  private readonly categoryService = inject(CategoryService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  productForm!: FormGroup;
  categories: Category[] = [];
  errorMessage: string | null = null;
  isSubmitting = false;
  isEditMode = false;
  productId: number | null = null;
  isLoading = true;

  ngOnInit(): void {
    this.initForm();
    this.loadCategories();

    // Check if we're in edit mode by looking for :id in the route
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.isEditMode = true;
      this.productId = +idParam;
      this.loadProduct(this.productId);
    } else {
      this.isLoading = false;
    }
  }

  private initForm(): void {
    this.productForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(200)]],
      description: ['', Validators.maxLength(2000)],
      price: [0, [Validators.required, Validators.min(0.01)]],
      categoryId: [null, Validators.required],
      stockQuantity: [0, [Validators.required, Validators.min(0)]]
    });
  }

  private loadCategories(): void {
    this.categoryService.getCategories().subscribe({
      next: (data) => this.categories = data,
      error: () => this.errorMessage = 'Failed to load categories.'
    });
  }

  private loadProduct(id: number): void {
    this.productService.getProduct(id).subscribe({
      next: (product) => {
        this.productForm.patchValue({
          name: product.name,
          description: product.description,
          price: product.price,
          categoryId: product.categoryId,
          stockQuantity: product.stockQuantity
        });
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = 'Failed to load product.';
        this.isLoading = false;
      }
    });
  }

  onSubmit(): void {
    if (this.productForm.invalid) {
      this.productForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = null;

    if (this.isEditMode && this.productId) {
      this.productService.updateProduct(this.productId, this.productForm.value).subscribe({
        next: () => this.router.navigate(['/']),
        error: (err) => {
          this.errorMessage = err.error?.title || 'Failed to update product.';
          this.isSubmitting = false;
        }
      });
    } else {
      this.productService.createProduct(this.productForm.value).subscribe({
        next: () => this.router.navigate(['/']),
        error: (err) => {
          this.errorMessage = err.error?.title || 'Failed to create product.';
          this.isSubmitting = false;
        }
      });
    }
  }
}
