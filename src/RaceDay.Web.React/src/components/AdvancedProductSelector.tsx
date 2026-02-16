import React, { useEffect, useState, useCallback } from 'react';
import type { ProductInfo } from '../types';
import { api } from '../api';

interface AdvancedProductSelectorProps {
  onProductsSelected: (products: ProductInfo[]) => void;
}

type ProductGroup = 'drink' | 'gel' | 'bar' | 'chew' | 'recovery';

const GROUP_LABELS: Record<ProductGroup, string> = {
  drink: '🥤 Drinks',
  gel: '🟦 Gels',
  bar: '🍫 Bars',
  chew: '🍬 Chews',
  recovery: '💊 Recovery (Post-Race)'
};

const GROUP_ORDER: ProductGroup[] = ['drink', 'gel', 'bar', 'chew', 'recovery'];

export const AdvancedProductSelector: React.FC<AdvancedProductSelectorProps> = ({ onProductsSelected }) => {
  const [products, setProducts] = useState<ProductInfo[]>([]);
  const [brands, setBrands] = useState<Set<string>>(new Set());
  const [selectedBrand, setSelectedBrand] = useState<string | null>(null);
  const [selectedProducts, setSelectedProducts] = useState<Set<string>>(new Set());
  const [searchQuery, setSearchQuery] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Load products on mount
  useEffect(() => {
    const loadProducts = async () => {
      try {
        setLoading(true);
        const data = await api.getProducts();
        setProducts(data);

        // Extract unique brands
        const uniqueBrands = new Set(data.map(p => p.brand).filter(Boolean));
        setBrands(uniqueBrands);

        // Select first brand by default
        const firstBrand = Array.from(uniqueBrands).sort((a, b) => a.localeCompare(b))[0];
        if (firstBrand) {
          setSelectedBrand(firstBrand);
          // Auto-select all products from first brand (excluding recovery)
          const brandProducts = data.filter(
            p => p.brand === firstBrand && p.productType !== 'recovery'
          );
          setSelectedProducts(new Set(brandProducts.map(p => p.id)));
        }

        setError(null);
      } catch (err) {
        setError('Failed to load products');
        console.error('Error loading products:', err);
      } finally {
        setLoading(false);
      }
    };

    loadProducts();
  }, []);

  // Update parent whenever selection changes
  const updateParent = useCallback((selectedIds: Set<string>) => {
    const selected = products.filter(p => selectedIds.has(p.id));
    onProductsSelected(selected);
  }, [products, onProductsSelected]);

  useEffect(() => {
    updateParent(selectedProducts);
  }, [selectedProducts, updateParent]);

  // Filter products by brand and search
  const filteredProducts = products.filter(p => {
    if (selectedBrand && p.brand !== selectedBrand) return false;
    if (searchQuery) {
      const query = searchQuery.toLowerCase();
      return (
        p.name.toLowerCase().includes(query) ||
        p.brand.toLowerCase().includes(query) ||
        p.productType.toLowerCase().includes(query)
      );
    }
    return true;
  });

  // Group products by type
  const groupedProducts = GROUP_ORDER.map(group => ({
    type: group,
    label: GROUP_LABELS[group],
    products: filteredProducts.filter(p => p.productType === group)
  })).filter(g => g.products.length > 0);

  const toggleProduct = (productId: string) => {
    const updated = new Set(selectedProducts);
    if (updated.has(productId)) {
      updated.delete(productId);
    } else {
      updated.add(productId);
    }
    setSelectedProducts(updated);
  };

  const selectAllInGroup = (group: ProductGroup) => {
    const groupProductIds = filteredProducts
      .filter(p => p.productType === group)
      .map(p => p.id);
    const updated = new Set(selectedProducts);
    groupProductIds.forEach(id => updated.add(id));
    setSelectedProducts(updated);
  };

  const deselectAllInGroup = (group: ProductGroup) => {
    const groupProductIds = new Set(
      filteredProducts.filter(p => p.productType === group).map(p => p.id)
    );
    const updated = new Set(Array.from(selectedProducts).filter(id => !groupProductIds.has(id)));
    setSelectedProducts(updated);
  };

  const handleBrandChange = (brand: string) => {
    setSelectedBrand(brand);
    // Auto-select all non-recovery products from the new brand
    const brandProducts = products.filter(
      p => p.brand === brand && p.productType !== 'recovery'
    );
    setSelectedProducts(new Set(brandProducts.map(p => p.id)));
  };

  if (loading) {
    return <div className="form-card"><p className="loading">Loading products...</p></div>;
  }

  const selectedCount = selectedProducts.size;

  return (
    <div className="form-card">
      <div className="selector-header">
        <h2>Nutrition Products</h2>
        <span className="badge">{selectedCount} selected</span>
      </div>

      {error && <p className="error-message">{error}</p>}

      {/* Brand Filter */}
      <div className="form-group">
        <label htmlFor="brand-filter">Brand Filter</label>
        <select
          id="brand-filter"
          value={selectedBrand || ''}
          onChange={(e) => handleBrandChange(e.target.value)}
          className="form-select"
        >
          <option value="">All Brands</option>
          {Array.from(brands).sort((a, b) => a.localeCompare(b)).map(brand => (
            <option key={brand} value={brand}>{brand}</option>
          ))}
        </select>
      </div>

      {/* Search */}
      <div className="form-group">
        <label htmlFor="product-search">Search Products</label>
        <input
          id="product-search"
          type="text"
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          placeholder="Search by name, brand, or type..."
          className="form-input"
        />
      </div>

      {/* Product Groups */}
      <div className="product-groups">
        {groupedProducts.map(group => {
          const groupSelected = group.products.filter(p => selectedProducts.has(p.id)).length;
          const groupTotal = group.products.length;
          const allSelected = groupSelected === groupTotal;

          return (
            <div key={group.type} className="product-group">
              <div className="group-header">
                <span className="group-label">{group.label}</span>
                <span className="group-count">{groupSelected}/{groupTotal}</span>
                <div className="group-actions">
                  {!allSelected && (
                    <button
                      className="btn-link"
                      onClick={() => selectAllInGroup(group.type)}
                    >
                      Select All
                    </button>
                  )}
                  {groupSelected > 0 && (
                    <button
                      className="btn-link"
                      onClick={() => deselectAllInGroup(group.type)}
                    >
                      Clear
                    </button>
                  )}
                </div>
              </div>
              <div className="product-list">
                {group.products.map(product => (
                  <label key={product.id} className="product-item">
                    <input
                      type="checkbox"
                      checked={selectedProducts.has(product.id)}
                      onChange={() => toggleProduct(product.id)}
                    />
                    <span className="product-details">
                      <span className="product-name">{product.name}</span>
                      <span className="product-meta">
                        {product.carbsG.toFixed(0)}g carbs
                        {product.caffeineMg && product.caffeineMg > 0 && (
                          <> • ☕ {product.caffeineMg}mg</>
                        )}
                      </span>
                    </span>
                  </label>
                ))}
              </div>
            </div>
          );
        })}
        {groupedProducts.length === 0 && (
          <p className="empty-message">No products found matching your criteria</p>
        )}
      </div>

      {selectedProducts.size === 0 && (
        <p className="warning-message">⚠️ Please select at least one product</p>
      )}
    </div>
  );
};
