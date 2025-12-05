import React, { useEffect, useState, useCallback } from 'react';
import type { ProductInfo } from '../types';
import { api } from '../api';

interface BrandSelectorProps {
  onBrandsSelected: (products: ProductInfo[]) => void;
}

export const BrandSelector: React.FC<BrandSelectorProps> = ({ onBrandsSelected }) => {
  const [products, setProducts] = useState<ProductInfo[]>([]);
  const [brands, setBrands] = useState<Set<string>>(new Set());
  const [selectedBrands, setSelectedBrands] = useState<Set<string>>(new Set());
  const [selectedTypes, setSelectedTypes] = useState<Set<string>>(new Set(['gel', 'drink', 'chew', 'bar']));
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const productTypes = ['gel', 'drink', 'chew', 'bar'];

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
        setSelectedBrands(new Set(uniqueBrands)); // Select all brands by default
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
  const updateParent = useCallback((brands: Set<string>, types: Set<string>) => {
    const filtered = products.filter(
      p => (brands.size === 0 || brands.has(p.brand)) && types.has(p.productType)
    );
    onBrandsSelected(filtered);
  }, [products, onBrandsSelected]);

  useEffect(() => {
    updateParent(selectedBrands, selectedTypes);
  }, [selectedBrands, selectedTypes, updateParent]);

  const toggleBrand = (brand: string) => {
    const updated = new Set(selectedBrands);
    if (updated.has(brand)) {
      updated.delete(brand);
    } else {
      updated.add(brand);
    }
    setSelectedBrands(updated);
  };

  const toggleType = (type: string) => {
    const updated = new Set(selectedTypes);
    if (updated.has(type)) {
      updated.delete(type);
    } else {
      updated.add(type);
    }
    setSelectedTypes(updated);
  };

  const selectAllBrands = () => {
    setSelectedBrands(new Set(brands));
  };

  const deselectAllBrands = () => {
    setSelectedBrands(new Set());
  };

  if (loading) {
    return <div className="form-card"><p className="loading">Loading brands...</p></div>;
  }

  return (
    <div className="form-card">
      <h2>Available Products</h2>
      
      {error && <p className="error-message">{error}</p>}

      {/* Product Types */}
      <div className="form-group">
        <label htmlFor="product-types-group">Product Types</label>
        <div id="product-types-group" className="checkbox-group">
          {productTypes.map(type => (
            <label key={type} className="checkbox-label">
              <input
                type="checkbox"
                checked={selectedTypes.has(type)}
                onChange={() => toggleType(type)}
              />
              <span>{type.charAt(0).toUpperCase() + type.slice(1)}</span>
            </label>
          ))}
        </div>
      </div>

      {/* Brands */}
      <div className="form-group">
        <div className="label-with-buttons">
          <label htmlFor="brands-group">Brands</label>
          <div className="button-group">
            <button onClick={selectAllBrands} className="link-button">Select All</button>
            <button onClick={deselectAllBrands} className="link-button">Deselect All</button>
          </div>
        </div>
        <div id="brands-group" className="checkbox-group">
          {Array.from(brands).sort((a, b) => a.localeCompare(b)).map(brand => (
            <label key={brand} className="checkbox-label">
              <input
                type="checkbox"
                checked={selectedBrands.has(brand)}
                onChange={() => toggleBrand(brand)}
              />
              <span>{brand}</span>
            </label>
          ))}
        </div>
      </div>

      <p className="info-text">
        {selectedBrands.size} brand{selectedBrands.size === 1 ? '' : 's'} selected • 
        {' '}{selectedTypes.size} product type{selectedTypes.size === 1 ? '' : 's'} selected
      </p>
    </div>
  );
};
