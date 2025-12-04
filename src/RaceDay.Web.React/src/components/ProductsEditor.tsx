import React from 'react';
import type { ProductEditor } from '../types';

interface ProductsEditorProps {
  gels: ProductEditor[];
  drinks: ProductEditor[];
  onGelsChange: (gels: ProductEditor[]) => void;
  onDrinksChange: (drinks: ProductEditor[]) => void;
}

export const ProductsEditor: React.FC<ProductsEditorProps> = ({
  gels,
  drinks,
  onGelsChange,
  onDrinksChange
}) => {
  const addGel = () => {
    onGelsChange([...gels, { name: '', carbsG: 0, sodiumMg: 0, volumeMl: 0 }]);
  };

  const removeGel = (index: number) => {
    if (gels.length > 1) {
      onGelsChange(gels.filter((_, i) => i !== index));
    }
  };

  const updateGel = (index: number, field: keyof ProductEditor, value: string | number) => {
    const updated = [...gels];
    updated[index] = { ...updated[index], [field]: value };
    onGelsChange(updated);
  };

  const addDrink = () => {
    onDrinksChange([...drinks, { name: '', carbsG: 0, sodiumMg: 0, volumeMl: 500 }]);
  };

  const removeDrink = (index: number) => {
    if (drinks.length > 1) {
      onDrinksChange(drinks.filter((_, i) => i !== index));
    }
  };

  const updateDrink = (index: number, field: keyof ProductEditor, value: string | number) => {
    const updated = [...drinks];
    updated[index] = { ...updated[index], [field]: value };
    onDrinksChange(updated);
  };

  return (
    <div className="form-card">
      <h2>🥤 Available Products</h2>
      
      <div className="products-section">
        <h3>Gels</h3>
        {gels.length === 0 ? (
          <p className="empty-message">No gels added yet</p>
        ) : (
          <div className="table-responsive">
            <table className="products-table">
              <thead>
                <tr>
                  <th>Product Name</th>
                  <th className="text-right">Carbs (g)</th>
                  <th className="text-right">Sodium (mg)</th>
                  <th className="text-center" style={{ width: '50px' }}></th>
                </tr>
              </thead>
              <tbody>
                {gels.map((gel, index) => (
                  <tr key={index}>
                    <td>
                      <input
                        type="text"
                        value={gel.name}
                        onChange={(e) => updateGel(index, 'name', e.target.value)}
                        placeholder="Product name"
                        className="form-control form-control-sm"
                      />
                    </td>
                    <td className="text-right">
                      <input
                        type="number"
                        value={gel.carbsG}
                        onChange={(e) => updateGel(index, 'carbsG', Number.parseFloat(e.target.value) || 0)}
                        className="form-control form-control-sm"
                        min="0"
                        step="0.1"
                        style={{ textAlign: 'right' }}
                      />
                    </td>
                    <td className="text-right">
                      <input
                        type="number"
                        value={gel.sodiumMg}
                        onChange={(e) => updateGel(index, 'sodiumMg', Number.parseFloat(e.target.value) || 0)}
                        className="form-control form-control-sm"
                        min="0"
                        step="1"
                        style={{ textAlign: 'right' }}
                      />
                    </td>
                    <td className="text-center">
                      <button
                        onClick={() => removeGel(index)}
                        className="btn btn-sm btn-icon"
                        title="Remove product"
                        disabled={gels.length <= 1}
                      >
                        🗑️
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
        <button onClick={addGel} className="btn btn-sm btn-secondary">+ Add Gel</button>
      </div>

      <div className="products-section">
        <h3>Drinks</h3>
        {drinks.length === 0 ? (
          <p className="empty-message">No drinks added yet</p>
        ) : (
          <div className="table-responsive">
            <table className="products-table">
              <thead>
                <tr>
                  <th>Product Name</th>
                  <th className="text-right">Carbs (g)</th>
                  <th className="text-right">Sodium (mg)</th>
                  <th className="text-right">Volume (ml)</th>
                  <th className="text-center" style={{ width: '50px' }}></th>
                </tr>
              </thead>
              <tbody>
                {drinks.map((drink, index) => (
                  <tr key={index}>
                    <td>
                      <input
                        type="text"
                        value={drink.name}
                        onChange={(e) => updateDrink(index, 'name', e.target.value)}
                        placeholder="Product name"
                        className="form-control form-control-sm"
                      />
                    </td>
                    <td className="text-right">
                      <input
                        type="number"
                        value={drink.carbsG}
                        onChange={(e) => updateDrink(index, 'carbsG', Number.parseFloat(e.target.value) || 0)}
                        className="form-control form-control-sm"
                        min="0"
                        step="0.1"
                        style={{ textAlign: 'right' }}
                      />
                    </td>
                    <td className="text-right">
                      <input
                        type="number"
                        value={drink.sodiumMg}
                        onChange={(e) => updateDrink(index, 'sodiumMg', Number.parseFloat(e.target.value) || 0)}
                        className="form-control form-control-sm"
                        min="0"
                        step="1"
                        style={{ textAlign: 'right' }}
                      />
                    </td>
                    <td className="text-right">
                      <input
                        type="number"
                        value={drink.volumeMl}
                        onChange={(e) => updateDrink(index, 'volumeMl', Number.parseFloat(e.target.value) || 0)}
                        className="form-control form-control-sm"
                        min="50"
                        step="50"
                        style={{ textAlign: 'right' }}
                      />
                    </td>
                    <td className="text-center">
                      <button
                        onClick={() => removeDrink(index)}
                        className="btn btn-sm btn-icon"
                        title="Remove product"
                        disabled={drinks.length <= 1}
                      >
                        🗑️
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
        <button onClick={addDrink} className="btn btn-sm btn-secondary">+ Add Drink</button>
      </div>
    </div>
  );
};
