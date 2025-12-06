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
  const [showAssortment, setShowAssortment] = useState(false);
  const [assortmentBrand, setAssortmentBrand] = useState<string | null>(null);

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
        // Select first brand by default (single selection)
        const firstBrand = Array.from(uniqueBrands).sort((a, b) => a.localeCompare(b))[0];
        setSelectedBrands(firstBrand ? new Set([firstBrand]) : new Set());
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
    // For single selection, if clicking the same brand, deselect it; otherwise select only that brand
    if (selectedBrands.has(brand) && selectedBrands.size === 1) {
      setSelectedBrands(new Set());
    } else {
      setSelectedBrands(new Set([brand]));
    }
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
        <label htmlFor="brands-group">Brand (Select One)</label>
        <div id="brands-group" className="radio-group">
          {Array.from(brands).sort((a, b) => a.localeCompare(b)).map(brand => (
            <label key={brand} className="radio-label">
              <input
                type="radio"
                name="brand-selection"
                checked={selectedBrands.has(brand)}
                onChange={() => toggleBrand(brand)}
              />
              <span>{brand}</span>
            </label>
          ))}
        </div>
      </div>

      <p className="info-text">
        {selectedBrands.size > 0 ? `${Array.from(selectedBrands)[0]} selected` : 'No brand selected'} • 
        {' '}{selectedTypes.size} product type{selectedTypes.size === 1 ? '' : 's'} selected
      </p>

      {/* View Assortment Button */}
      {selectedBrands.size > 0 && (
        <button
          className="btn btn-secondary"
          onClick={() => {
            setAssortmentBrand(Array.from(selectedBrands)[0]);
            setShowAssortment(true);
          }}
          onKeyDown={(e) => {
            if (e.key === 'Enter' || e.key === ' ') {
              e.preventDefault();
              setAssortmentBrand(Array.from(selectedBrands)[0]);
              setShowAssortment(true);
            }
          }}
          style={{ marginTop: '12px', width: '100%' }}
        >
          🛍️ View All {Array.from(selectedBrands)[0]} Products
        </button>
      )}

      {/* Assortment Modal */}
      {showAssortment && assortmentBrand && (
        <div 
          className="modal-overlay" 
          onClick={() => setShowAssortment(false)}
          role="dialog"
          aria-modal="true"
          aria-label="Brand assortment modal"
          onKeyDown={(e) => {
            if (e.key === 'Escape') setShowAssortment(false);
          }}
        >
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h2>{assortmentBrand} - Full Assortment</h2>
              <button className="modal-close" onClick={() => setShowAssortment(false)}>✕</button>
            </div>
            
            <div className="modal-body">
              <div className="assortment-grid">
                {products
                  .filter(p => p.brand === assortmentBrand)
                  .sort((a, b) => a.productType.localeCompare(b.productType))
                  .map((product) => (
                    <div key={product.name} className="assortment-item">
                      <div className="item-header">
                        <strong>{product.name}</strong>
                      </div>
                      <div className="item-details">
                        <span className="type">{product.productType}</span>
                        <span className="carbs">{product.carbsG}g carbs</span>
                      </div>
                      {product.sodiumMg > 0 && (
                        <div className="item-sodium">
                          <span>{product.sodiumMg}mg sodium</span>
                        </div>
                      )}
                    </div>
                  ))}
              </div>
            </div>

            <div className="modal-footer">
              <button className="btn btn-primary" onClick={() => setShowAssortment(false)}>Close</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
