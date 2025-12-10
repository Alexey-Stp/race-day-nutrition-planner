import React, { useEffect, useState } from 'react';
import type { ProductInfo } from '../types';
import { api } from '../api';

interface ProductsListProps {
  onProductSelected?: (product: ProductInfo) => void;
}

export const ProductsList: React.FC<ProductsListProps> = ({ onProductSelected }) => {
  const [products, setProducts] = useState<ProductInfo[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedType, setSelectedType] = useState<string>('');
  const [selectedBrand, setSelectedBrand] = useState<string>('');

  // Load products on mount
  useEffect(() => {
    const loadProducts = async () => {
      try {
        setLoading(true);
        const data = await api.getProducts();
        setProducts(data);
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

  // Filter products based on selections
  const filteredProducts = products.filter(p => {
    const typeMatch = !selectedType || p.productType === selectedType;
    const brandMatch = !selectedBrand || p.brand === selectedBrand;
    return typeMatch && brandMatch;
  });

  // Get unique types and brands for filters
  const types = Array.from(new Set(products.map(p => p.productType))).sort((a, b) => a.localeCompare(b));
  const brands = Array.from(new Set(products.map(p => p.brand))).sort((a, b) => a.localeCompare(b));

  if (loading) {
    return <div className="products-list-container"><p>Loading products...</p></div>;
  }

  if (error) {
    return <div className="products-list-container error"><p>{error}</p></div>;
  }

  return (
    <div className="products-list-container">
      <h2>Available Products</h2>
      
      <div className="products-filters">
        <div className="filter-group">
          <label htmlFor="product-type-filter">Product Type:</label>
          <select id="product-type-filter" value={selectedType} onChange={(e) => setSelectedType(e.target.value)}>
            <option value="">All Types</option>
            {types.map(type => (
              <option key={type} value={type}>{type.charAt(0).toUpperCase() + type.slice(1)}</option>
            ))}
          </select>
        </div>
        
        <div className="filter-group">
          <label htmlFor="brand-filter">Brand:</label>
          <select id="brand-filter" value={selectedBrand} onChange={(e) => setSelectedBrand(e.target.value)}>
            <option value="">All Brands</option>
            {brands.map(brand => (
              <option key={brand} value={brand}>{brand}</option>
            ))}
          </select>
        </div>
      </div>

      <div className="products-list-wrapper">
        {filteredProducts.length === 0 ? (
          <p className="no-products">No products found</p>
        ) : (
          <table className="products-list-table">
            <thead>
              <tr>
                <th>Product</th>
                <th>Brand</th>
                <th>Type</th>
                <th className="text-right">Carbs (g)</th>
                <th className="text-right">Sodium (mg)</th>
                <th className="text-right">Calories</th>
                <th className="text-right">Volume (ml)</th>
                <th>Caffeine (mg)</th>
                {onProductSelected && <th>Action</th>}
              </tr>
            </thead>
            <tbody>
              {filteredProducts.map(product => (
                <tr key={product.id}>
                  <td className="product-name">{product.name}</td>
                  <td>{product.brand}</td>
                  <td>{product.productType.charAt(0).toUpperCase() + product.productType.slice(1)}</td>
                  <td className="text-right">{product.carbsG.toFixed(1)}</td>
                  <td className="text-right">{product.sodiumMg.toFixed(0)}</td>
                  <td className="text-right">{product.caloriesKcal.toFixed(0)}</td>
                  <td className="text-right">{product.volumeMl > 0 ? product.volumeMl.toFixed(0) : '-'}</td>
                  <td>{product.caffeineMg ? `☕ ${product.caffeineMg.toFixed(0)}` : '-'}</td>
                  {onProductSelected && (
                    <td>
                      <button
                        className="btn btn-sm btn-primary"
                        onClick={() => onProductSelected(product)}
                      >
                        Add
                      </button>
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      <div className="products-list-summary">
        <p>Showing {filteredProducts.length} of {products.length} products</p>
      </div>
    </div>
  );
};
