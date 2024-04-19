interface Array<T> {
  Any(predicate?: (item: T) => boolean): boolean;
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
   * @param {(item: T) => K} mapper The function to get inner collection from each element
   * @returns {Array<K>} Returns flattened array
   */
  SelectMany(mapper: Function<T, K[]>): Array<K>;
  /**
   * @template T, K
   * @param {(item: T) => K} mapper The function to get inner collection from each element
   * @returns {Array<K>} Returns flattened array
   */
  SelectForEach(mapper: Function<T, K[]>): Array<K>;
  HasElement(): boolean;
  Nothing(): boolean;
  Remove(item: T): void;
  /**
   * @param {Function<T>} getChildren - Mapper method to get inner property
   * @returns {Array<T>} The flatterned array
   */
  Flattern(getChildren: (value: T, index: number, array: T[]) => void, thisArg?: any): T[];
  /**
   * @template T, K, L
   * @param keySelector 
   * @param {(item: T) => L} valueSelector 
   * @return {{ [key: K] : L}}
   */
  ToDictionary(keySelector: (item: T) => K, valueSelector: (item: T) => L): { [key: K]: L };
  
  /**
 * Returns the first element of the array that satisfies the specified condition, or an empty array if no such element is found.
 * @param filter - A function that tests each element for a condition.
 * @returns The first element of the array that passes the test implemented by the provided function, or an empty array if no element passes the test.
 */
  FirstOrDefault(filter: (item: T, index: number) => boolean): Array<T>;
}

interface String {
  HasAnyChar(): boolean;
  HasElement(): boolean;
  HasNonSpaceChar(): boolean;
  IsNullOrWhiteSpace(): boolean;
}