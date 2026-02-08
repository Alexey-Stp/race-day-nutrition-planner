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
        <div // NOSONAR - Standard modal overlay pattern with click-to-dismiss
          className="modal-overlay" 
          onClick={() => setShowAssortment(false)}
        >
          <div // NOSONAR - Dialog content stops event propagation, standard modal pattern
            className="modal-content" 
            onClick={(e) => e.stopPropagation()}
            role="dialog"
            aria-modal="true"
            aria-labelledby="modal-title"
          >
            <div className="modal-header">
              <h2 id="modal-title">{assortmentBrand} - Full Assortment</h2>
              <button 
                className="modal-close" 
                onClick={() => setShowAssortment(false)}
                onKeyDown={(e) => {
                  if (e.key === 'Escape') setShowAssortment(false);
                }}
                aria-label="Close modal"
              >
                ✕
              </button>
            </div>
            
            <div className="modal-body">
              <table className="products-table">
                <thead>
                  <tr>
                    <th>Product</th>
                    <th>Type</th>
                    <th className="text-right">Carbs (g)</th>
                    <th className="text-right">Sodium (mg)</th>
                    <th className="text-right">Caffeine (mg)</th>
                  </tr>
                </thead>
                <tbody>
                  {products
                    .filter(p => p.brand === assortmentBrand)
                    .sort((a, b) => a.productType.localeCompare(b.productType))
                    .map((product) => (
                      <tr key={product.name}>
                        <td className="product-name">{product.name}</td>
                        <td>{product.productType}</td>
                        <td className="text-right">{product.carbsG.toFixed(1)}</td>
                        <td className="text-right">{product.sodiumMg.toFixed(0)}</td>
                        <td className="text-right">{product.caffeineMg ? product.caffeineMg.toFixed(0) : '-'}</td>
                      </tr>
                    ))}
                </tbody>
              </table>
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
