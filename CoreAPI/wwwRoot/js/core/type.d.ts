interface Array<T> {
  Contains(item: T): boolean;
  /**
   * @returns {Array<T>} the array itself
   */
  ToArray(): Array<T>;
  /**
   * @template T, K
   * @param {Function<T, K>} mapper - The map function
   * @returns {Array<K>} return the mappped array
   */
  Select(mapper: Function<T, K>): Array<K>;
  /**
   * @param filter The function to filter elements
   * @returns {T[]} Returns filtered array
   */
  Where(callbackfn: (value: T, index: number, array: T[]) => void, thisArg?: any): T[];
  /**
   * @template T, K
   * @param mapper The function to get inner collection from each element
   * @returns {Array<K>} Returns flattened array
   */
  SelectMany(mapper: Function<T, K[]>): Array<K>;
  HasElement(): boolean;
  Nothing(): boolean;
  Remove(item: T): void;
  /**
   * @param {Function<T>} getChildren - Mapper method to get inner property
   * @returns {Array<T>} The flatterned array
   */
  Flattern(getChildren: (value: T, index: number, array: T[]) => void, thisArg?: any): T[];
}

interface String {
  HasElement(): boolean;
  HasNonSpaceChar(): boolean;
  IsNullOrWhiteSpace(): boolean;
}