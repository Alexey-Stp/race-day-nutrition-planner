import React, { useEffect, useState, useCallback } from 'react';
import type { ProductInfo } from '../types';
import { api } from '../api';

interface ProductSelectorProps {
  onProductAdded: (product: ProductInfo) => void;
}

export const ProductSelector: React.FC<ProductSelectorProps> = ({ onProductAdded }) => {
  const [products, setProducts] = useState<ProductInfo[]>([]);
  const [allProducts, setAllProducts] = useState<ProductInfo[]>([]);
  const [availableBrands, setAvailableBrands] = useState<string[]>([]);
  const [selectedBrand, setSelectedBrand] = useState('');
  const [showGels, setShowGels] = useState(true);
  const [showDrinks, setShowDrinks] = useState(true);
  const [showBars, setShowBars] = useState(true);
  const [loading, setLoading] = useState(true);

  const extractBrands = useCallback((products: ProductInfo[]) => {
    const brands = Array.from(new Set(products.map(p => p.brand)))
      .sort((a, b) => a.localeCompare(b));
    setAvailableBrands(brands);
  }, []);

  const loadProducts = useCallback(async () => {
    try {
      const data = await api.getProducts();
      setAllProducts(data);
      extractBrands(data);
    } catch (error) {
      console.error('Error loading products:', error);
    } finally {
      setLoading(false);
    }
  }, [extractBrands]);

  const filterProducts = useCallback(() => {
    let filtered = allProducts;

    // Filter by brand
    if (selectedBrand) {
      filtered = filtered.filter(p => p.brand === selectedBrand);
    }

    // Filter by product types
    const typeFilter: string[] = [];
    if (showGels) typeFilter.push('gel');
    if (showDrinks) typeFilter.push('drink');
    if (showBars) typeFilter.push('bar');

    if (typeFilter.length > 0) {
      filtered = filtered.filter(p => typeFilter.includes(p.productType));
    }

    setProducts(filtered);
  }, [allProducts, selectedBrand, showGels, showDrinks, showBars]);

  useEffect(() => {
    loadProducts();
  }, [loadProducts]);

  useEffect(() => {
    filterProducts();
  }, [filterProducts]);

  return (
    <div className="product-selector">
      <h3>Select Products for Your Race</h3>
      
      <div className="filter-section">
        <div className="filter-group">
          <label htmlFor="brand-select">Brand:</label>
          <select id="brand-select" value={selectedBrand} onChange={(e) => setSelectedBrand(e.target.value)}>
            <option value="">All Brands</option>
            {availableBrands.map(brand => (
              <option key={brand} value={brand}>{brand}</option>
            ))}
          </select>
        </div>
        
        <div className="filter-group">
          <fieldset>
            <legend>Product Types:</legend>
            <div className="checkbox-group">
            <label className="checkbox-label">
              <input
                type="checkbox"
                checked={showGels}
                onChange={(e) => setShowGels(e.target.checked)}
              />
              <span>Gels</span>
            </label>
            <label className="checkbox-label">
              <input
                type="checkbox"
                checked={showDrinks}
                onChange={(e) => setShowDrinks(e.target.checked)}
              />
              <span>Drinks</span>
            </label>
            <label className="checkbox-label">
              <input
                type="checkbox"
                checked={showBars}
                onChange={(e) => setShowBars(e.target.checked)}
              />
              <span>Bars</span>
            </label>
          </div>
        </fieldset>
        </div>
      </div>

      {loading ? (
        <p className="loading">Loading products...</p>
      ) : products.length === 0 ? (
        <p className="no-products">No products found</p>
      ) : (
        <div className="products-grid">
          {products.map(product => (
            <div key={product.id} className="product-card">
              <div className="product-header">
                <h4>{product.name}</h4>
                <span className="brand-badge">{product.brand}</span>
              </div>
              <div className="product-type">{product.productType}</div>
              <div className="product-info">
                <div className="info-row">
                  <span className="label">Calories:</span>
                  <span className="value">{product.caloriesKcal} kcal</span>
                </div>
                <div className="info-row">
                  <span className="label">Carbs:</span>
                  <span className="value">{product.carbsG} g</span>
                </div>
                <div className="info-row">
                  <span className="label">Sodium:</span>
                  <span className="value">{product.sodiumMg} mg</span>
                </div>
                <div className="info-row">
                  <span className="label">Volume:</span>
                  <span className="value">{product.volumeMl} ml</span>
                </div>
              </div>
              <button className="add-btn" onClick={() => onProductAdded(product)}>
                Add to Plan
              </button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};
